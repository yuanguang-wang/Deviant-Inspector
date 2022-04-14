using Rhino;
using System;

namespace Deviant_Inspector
{
    public class Deviant_InspectorPlugin : Rhino.PlugIns.PlugIn
    {
        public Deviant_InspectorPlugin()
        {
            Instance = this;
        }
        public static Deviant_InspectorPlugin Instance { get; private set; }
    }
}