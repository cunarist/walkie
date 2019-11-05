using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace RhinoWASD
{
    public class WASD : Command
    {
        public WASD(){  Instance = this;    }
        
        public static WASD Instance{    get; private set;   }
        
        public override string EnglishName{ get { return "WASD"; }  }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            GetOption go = new GetOption();
            go.AcceptNothing(true);
     
            OptionToggle infoAtStartup = new OptionToggle(Properties.Settings.Default.InfoAtStartup, "Hide" , "Show");
            go.AddOptionToggle("ShowInfo", ref infoAtStartup);
            go.SetCommandPrompt("Start first-person navigation");
            GetResult res = go.Get();
            while (res == GetResult.Option)
                res = go.Get();

            if(res != GetResult.Nothing)
                return Result.Cancel;

            Properties.Settings.Default.InfoAtStartup = infoAtStartup.CurrentValue;
            Properties.Settings.Default.Save();
            Interceptor.StartWASD(infoAtStartup.CurrentValue);

            return Result.Success;
        }
    }
}