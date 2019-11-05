namespace RhinoWASD
{
    public class PlugIn : Rhino.PlugIns.PlugIn
    {
        public PlugIn(){    Instance = this;    }
        
        public static PlugIn Instance{  get; private set;   }
    }
}