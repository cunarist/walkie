using Rhino.PlugIns;

namespace RhinoWASD
{
    public class PlugIn : Rhino.PlugIns.PlugIn
    {
        public override PlugInLoadTime LoadTime => PlugInLoadTime.AtStartup;

        public PlugIn() { Instance = this; }

        public static PlugIn Instance { get; private set; }

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            Rhino.ApplicationSettings.GeneralSettings.MiddleMouseMode = Rhino.ApplicationSettings.MiddleMouseMode.RunMacro;
            Rhino.ApplicationSettings.GeneralSettings.MiddleMouseMacro = "Walk";

            return LoadReturnCode.Success;
        }

        protected override void OnShutdown()
        {
        }
    }
}