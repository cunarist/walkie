using Display;
using Rhino;
using Rhino.Commands;
using Rhino.Display;

namespace RhinoWASD
{
    public class Walk : Command
    {
        public Walk() { Instance = this; }

        public static Walk Instance { get; private set; }

        public override string EnglishName { get { return "Walk"; } }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            RhinoViewport vp = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport;
            if (vp.IsParallelProjection)
            {
                Overlay.ShowMessage("Cannot walk in a parallel view");
                return Result.Cancel;
            }
            else
            {
                Interceptor.StartWASD();
                return Result.Success;
            }
        }
    }
}