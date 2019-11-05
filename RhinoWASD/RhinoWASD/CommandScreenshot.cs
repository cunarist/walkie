using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Drawing;

namespace RhinoWASD
{
    public class Screenshot : Command
    {
        private OptionInteger widthOpt;
        private OptionInteger heightOpt;
        private OptionInteger dpiOpt;
        private OptionToggle gridAxesToggle;
        private OptionToggle ratioToggle;
        private OptionToggle portraitToggle;

        public Screenshot(){ Instance = this; }
        
        public static Screenshot Instance {    get; private set;   }
        
        public override string EnglishName{ get { return "Screenshot"; }  }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            this.widthOpt = new OptionInteger(Properties.Settings.Default.Width);
            this.heightOpt = new OptionInteger(Properties.Settings.Default.Height);
            this.dpiOpt = new OptionInteger(Properties.Settings.Default.DPI);
            this.ratioToggle = new OptionToggle(Properties.Settings.Default.KeepRatio, "Yes", "No");
            this.gridAxesToggle = new OptionToggle(Properties.Settings.Default.GridAndAxes, "Hide", "Show");
            this.portraitToggle = new OptionToggle(Properties.Settings.Default.Portrait, "Landscape", "Portrait");

            GetOption go = new GetOption();
            go.AcceptNothing(true);

            int selectedResolution = Properties.Settings.Default.Resolution;
            this.RestoreOptions(ref go, selectedResolution);

            GetResult res = go.Get();
            while (res == GetResult.Option)
            {
                if (go.Option().CurrentListOptionIndex >= 0)
                    selectedResolution = go.Option().CurrentListOptionIndex;

                this.RestoreOptions(ref go, selectedResolution);
                res = go.Get();
            }

            if (res != GetResult.Nothing)
                return Result.Cancel;

            Properties.Settings.Default.Resolution = selectedResolution;
            Properties.Settings.Default.Width = widthOpt.CurrentValue;
            Properties.Settings.Default.Height = heightOpt.CurrentValue;
            Properties.Settings.Default.KeepRatio = ratioToggle.CurrentValue;
            Properties.Settings.Default.GridAndAxes = gridAxesToggle.CurrentValue;
            Properties.Settings.Default.Portrait = portraitToggle.CurrentValue;
            Properties.Settings.Default.Save();

            RhinoHelpers.CustomScreenshot();

            return Result.Success;
        }

        private void RestoreOptions(ref GetOption go, int ResolutionIndex)
        {
            Size size = RhinoHelpers.CalculateSize(
                ResolutionIndex,
                widthOpt.CurrentValue,
                heightOpt.CurrentValue,
                dpiOpt.CurrentValue,
                portraitToggle.CurrentValue
            );
            if (size.IsEmpty)
                size = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.Bounds.Size;

            go.SetCommandPrompt("Screenshot ("+size.Width+"x"+size.Height+")");
            go.ClearCommandOptions();
            go.AddOptionList("Resolution", new string[] { "FullHD", "4K", "A4", "A3", "View", "Custom" }, ResolutionIndex);

            // Show option width and height only if "Custom" is selected.
            if (ResolutionIndex == 5)
            {
                go.AddOptionInteger("Width", ref this.widthOpt);
                go.AddOptionInteger("Height", ref this.heightOpt);

                go.AddOptionToggle("KeepRatio", ref this.ratioToggle);
            }
            else if (ResolutionIndex >= 2 && ResolutionIndex <= 3)
            {
                go.AddOptionToggle("Orientation", ref this.portraitToggle);
                go.AddOptionInteger("DPI", ref this.dpiOpt);
            }

            // Show option Grid And Axes
            go.AddOptionToggle("GridAndAxes", ref this.gridAxesToggle);
        }
    }
}
