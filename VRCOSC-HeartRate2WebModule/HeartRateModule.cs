using VRCOSC.Game.Modules;
using VRCOSC.Game.Modules.Bases.Heartrate;

namespace HeartRateToWeb
{
    [ModuleTitle("HeartRateToWeb")]
    [ModuleDescription("Displays heartrate data from Trizen-based watches using HeartRateToWeb app")]
    [ModuleAuthor("BlackOfWorld")]
    [ModuleGroup(ModuleType.Health)]
    public partial class HeartRateModule : HeartrateModule<ModuleProvider>
    {      
        internal int Port => GetSetting<int>(Settings.Port);

        protected override void CreateAttributes()
        {
            base.CreateAttributes();
            CreateSetting(Settings.Port, "Port", "Port to listen to", 6547, 0, 65535);
        }

        protected override void OnModuleStart()
        {
            LogDebug("Starting module");
            base.OnModuleStart();
        }


        protected override void OnModuleStop()
        {
            LogDebug("Stopping module");
            base.OnModuleStop();
        }

        protected override ModuleProvider CreateProvider()
        {
            LogDebug("Creating provider");
            return new ModuleProvider(this);
        }
        internal new void Log(string message) => base.Log(message);
        internal new void LogDebug(string message) => base.LogDebug(message);
        internal new void PushException(Exception e) => base.PushException(e);

        private enum Settings
        {
            Port
        }
    }
}
