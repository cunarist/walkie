using Rhino;
using Rhino.Commands;

namespace RhinoWASD
{
    public class WASD : Command
    {
        public WASD() { Instance = this; }

        public static WASD Instance { get; private set; }

        public override string EnglishName { get { return "Walk"; } }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Interceptor.StartWASD();
            return Result.Success;
        }
    }

    public class EnableCursorZoomDepth : Command
    {
        public EnableCursorZoomDepth() { Instance = this; }

        public static EnableCursorZoomDepth Instance { get; private set; }

        public override string EnglishName { get { return "EnableCursorZoomDepth"; } }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            RhinoWASD.PlugIn.setDepthEnabled = true;
            return Result.Success;
        }
    }
}