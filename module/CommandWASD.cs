using Rhino;
using Rhino.Commands;

namespace RhinoWASD
{
    public class WASD : Command
    {
        public WASD() { Instance = this; }

        public static WASD Instance { get; private set; }

        public override string EnglishName { get { return "WASD"; } }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Interceptor.StartWASD();
            return Result.Success;
        }
    }
}