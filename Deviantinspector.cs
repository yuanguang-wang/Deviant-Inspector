using Rhino;
using System;
using System.Collections.Generic;

namespace Deviant_Inspector
{
    public class Deviantinspector : Rhino.Commands.Command
    {

        /// <summary>
        /// TO-DO List aka Command Index:
        ///     1.Absolutely Vertical (V);
        ///     2.Redundant Control Points (P);
        ///     3.Nearly Flat Surface (S);
        ///     4.Unexpected Duplication (D);
        ///     5.Run All Diagnosis (A);
        ///     6.Rollback and Release Criminals (R);
        ///     7.Set Criminal's Color (C);
        /// </summary>

        public Deviantinspector()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static Deviantinspector Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "Deviantinspector";

        protected override Rhino.Commands.Result RunCommand(RhinoDoc doc, Rhino.Commands.RunMode mode)
        {
            Rhino.Input.Custom.GetObject getObjects = new Rhino.Input.Custom.GetObject 
            {
                GeometryFilter = Rhino.DocObjects.ObjectType.Brep,
                GroupSelect = true,
                SubObjectSelect = false
            };            
            // Set Options
            Rhino.Input.Custom.OptionToggle abVerti = new Rhino.Input.Custom.OptionToggle(true, "Off", "On");
            Rhino.Input.Custom.OptionToggle redunCP = new Rhino.Input.Custom.OptionToggle(true, "Off", "On");
            Rhino.Input.Custom.OptionToggle fltSurf = new Rhino.Input.Custom.OptionToggle(true, "Off", "On");
            Rhino.Input.Custom.OptionToggle dupBrep = new Rhino.Input.Custom.OptionToggle(true, "Off", "On");
            // Remarks on method AddOptionToggle
            // Body: str Must only consist of letters and numbers (no characters list periods, spaces, or dashes))
            // Type OptionToggle need a ref prefix
            getObjects.AddOptionToggle("AbsolutelyVertical", ref abVerti);
            getObjects.AddOptionToggle("RedundantControlPoints", ref redunCP);
            getObjects.AddOptionToggle("NearlyFlatSurface", ref fltSurf);
            getObjects.AddOptionToggle("UnexpectedDuplication", ref dupBrep);
            // Data Collection           
            while (true)
            {
                Rhino.Input.GetResult rc = getObjects.GetMultiple(1,0);
                if (rc == Rhino.Input.GetResult.Option)
                {
                    continue;
                }
                else if (rc == Rhino.Input.GetResult.Object)
                {
                    RhinoApp.WriteLine("obj selected");
                }
                else
                {
                    RhinoApp.WriteLine("Selection has been Interrupted, Command Exit");
                    return Rhino.Commands.Result.Failure;
                }
                break;                
            }
            return Rhino.Commands.Result.Success;
        }
    }
}
