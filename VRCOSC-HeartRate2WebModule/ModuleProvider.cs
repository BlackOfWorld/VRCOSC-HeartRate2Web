using NuGet.Packaging;
using System.Net;
using System.Web;
using System.Windows;
using VRCOSC.Game.Modules;
using VRCOSC.Game.Modules.Bases.Heartrate;

namespace HeartRateToWeb
{
    public class ModuleProvider : HeartrateProvider
    {
        // It just means the server got a request...
        private bool hasSeenHeart = false;
        public override bool IsConnected => hasSeenHeart && httpListener.IsListening;

        protected override TimeSpan IsReceivingTimeout => TimeSpan.FromSeconds(10);

        public HeartRateModule mod;
        private HttpListener httpListener;

        public ModuleProvider(HeartRateModule mod)
        {
            this.mod = mod;
            this.httpListener = new HttpListener();
        }

        public override void Initialise()
        {
            mod.LogDebug("Initializing provider");
            base.Initialise();
            this.httpListener.Prefixes.AddRange(new[] { $"http://127.0.0.1:{mod.Port}/", $"http://localhost:{mod.Port}/" });
            mod.LogDebug("Added localhost to listener");
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (ip.AddressFamily.ToString() != "InterNetwork")
                    continue;
                var host = ip + ":" + mod.Port;
                this.httpListener.Prefixes.Add("http://" + host + "/");
                mod.LogDebug($"Added {host} to listener");
            }

            // This will throw an exception if user made an error with the port (or port is privileged and program does not have admin perms)
            try
            {
                httpListener.Start();
            }
            catch (HttpListenerException e)
            {
                if (e.NativeErrorCode == 5)
                {
                    for (int i = 0; i < 10; i++) mod.Log("RUN THIS PROGRAM AS ADMINISTRATOR!!!!");
                }
                mod.PushException(e);
                return;
            }
            WaitForRequest();
        }

        private void WaitForRequest()
        {
            this.httpListener.BeginGetContext(new AsyncCallback(serverCallback), this.httpListener);
        }

        private void serverCallback(IAsyncResult result)
        {
            if (!this.httpListener.IsListening) return;
            HttpListener listener = (HttpListener)result.AsyncState;
            HttpListenerContext ctx;
            try
            {
                ctx = listener!.EndGetContext(result);
            }
            catch (Exception ex)
            {
                mod.Log($"Exception happened during data transfer! Ex: {ex.Message}");
                return;
            }
            var response = ctx.Response;
            do
            {
                if (ctx.Request.HttpMethod != "POST") break;
                var data = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding).ReadToEnd();
                if (data == null) break;
                var hearRate = int.Parse(data.Split("=")[1]);

                mod.LogDebug($"Invoking OnHeartrateUpdate action with data {hearRate}");
                OnHeartrateUpdate!.Invoke(hearRate);
                byte[] ACK = new byte[] { (byte)'O', (byte)'K' };
                response.ContentLength64 = ACK.Length;
                response.OutputStream.Write(ACK, 0, ACK.Length);
                mod.LogDebug("Sent ACK!");
                hasSeenHeart = true;
            } while (false);


            response.OutputStream.Close();
            this.WaitForRequest();
        }
        public override Task Teardown()
        {
            mod.LogDebug("Teardown Provider");
            this.httpListener.Stop();
            return Task.CompletedTask;
        }
    }
}
