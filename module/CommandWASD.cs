using Rhino;
using Rhino.Commands;

namespace RhinoWASD
{
    public class Walk : Command
    {
        public Walk() { Instance = this; }

        public static Walk Instance { get; private set; }

        public override string EnglishName { get { return "Walk"; } }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Interceptor.StartWASD();
            return Result.Success;
        }
    }
}