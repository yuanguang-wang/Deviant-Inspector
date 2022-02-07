using Rhino;
using System;
using System.Collections.Generic;

namespace Deviant_Inspector
{
    public class Deviant_InspectorCommand : Rhino.Commands.Command
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

        public Deviant_InspectorCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static Deviant_InspectorCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "Deviantinspector";

        protected override Rhino.Commands.Result RunCommand(RhinoDoc doc, Rhino.Commands.RunMode mode)
        {
            // Initiation
            Rhino.Input.Custom.GetOption getOption = new Rhino.Input.Custom.GetOption();
            getOption.AcceptNothing(true);
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
            getOption.AddOptionToggle("AbsolutelyVertical", ref abVerti);
            getOption.AddOptionToggle("RedundantControlPoints", ref redunCP);
            getOption.AddOptionToggle("NearlyFlatSurface", ref fltSurf);
            getOption.AddOptionToggle("UnexpectedDuplication", ref dupBrep);

            // Option Collection
            string[] subCommandsAry = new string[] {"On","On","On","On"};
            getOption.SetCommandPrompt("Select the Inspector to be Excuted, Press Enter When Finish Setting");
            while (true)
            {
                Rhino.Input.GetResult rc = getOption.Get();
                if (getOption.CommandResult() != Rhino.Commands.Result.Success)
                {
                return getOption.CommandResult();
                }
                if (rc == Rhino.Input.GetResult.Nothing)
                {
                    RhinoApp.WriteLine("Inspector Setting Finished");
                    break;
                }
                else if (rc == Rhino.Input.GetResult.Option)
                {
                    int x = getOption.OptionIndex();
                    if (getOption.Option().StringOptionValue == "Off")
                    {
                        subCommandsAry[x - 1] = "Off";
                    }
                    continue;
                }
            }

            // Brep Collection
            getObjects.SetCommandPrompt("Select the B-Reps to be Inspected");
            getObjects.GetMultiple(1,0);
            if (getObjects.CommandResult() != Rhino.Commands.Result.Success)
            {
                RhinoApp.WriteLine("GetObject Method Failure, Command Exit");
                return Rhino.Commands.Result.Failure;
            }
            Rhino.DocObjects.ObjRef[] objs = getObjects.Objects();
            List<Rhino.Geometry.Brep> breps = new List<Rhino.Geometry.Brep>();
            List<Rhino.DocObjects.RhinoObject> rh_objs = new List<Rhino.DocObjects.RhinoObject>();
            foreach (Rhino.DocObjects.ObjRef obj in objs)
            {
                breps.Add(obj.Brep());
                rh_objs.Add(obj.Object());
            }

            // Run subCommand dependently
            foreach (string subCommand in subCommandsAry)
            {
                if (subCommand == "On")
                {

                }
            }
            foreach (Rhino.DocObjects.RhinoObject rhinoObject in rh_objs)
            {
                Method_Main.ObjAttrRevise(rhinoObject, "TestName|");
            }

            doc.Views.Redraw();

            return Rhino.Commands.Result.Success;
        }
    }
}
