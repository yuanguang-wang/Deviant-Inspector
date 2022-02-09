using Rhino;
using System;
using System.Collections.Generic;

using MM = Deviant_Inspector.Method_Main;

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
            Rhino.Input.Custom.GetObject getObjects = new Rhino.Input.Custom.GetObject 
            {
                GeometryFilter = Rhino.DocObjects.ObjectType.Brep,
                GroupSelect = true,
                SubObjectSelect = false
            };
            double modelTolerance = doc.ModelAbsoluteTolerance;
            int enlargeRatio = 100;

            // Set Options
            //Target: Rhino.Geometry.Surface
            Rhino.Input.Custom.OptionToggle abVerti = new Rhino.Input.Custom.OptionToggle(true, "Off", "On");
            //Target: Rhino.Geometry.Surface
            Rhino.Input.Custom.OptionToggle redunCP = new Rhino.Input.Custom.OptionToggle(true, "Off", "On");
            //Target: Rhino.Geometry.Surface
            Rhino.Input.Custom.OptionToggle fltSurf = new Rhino.Input.Custom.OptionToggle(true, "Off", "On");
            //Target: Rhino.Geometry.Brep
            Rhino.Input.Custom.OptionToggle dupBrep = new Rhino.Input.Custom.OptionToggle(true, "Off", "On");
            //Target: Rhino.Geometry.Surface
            Rhino.Input.Custom.OptionToggle extuCrv = new Rhino.Input.Custom.OptionToggle(true, "Off", "On");

            // Remarks on method AddOptionToggle
            // Body: str Must only consist of letters and numbers (no characters list periods, spaces, or dashes))
            // Type OptionToggle need a ref prefix
            getOption.AddOptionToggle("Absolutely_Vertical", ref abVerti);
            getOption.AddOptionToggle("Redundant_Control_Points", ref redunCP);
            getOption.AddOptionToggle("Nearly_Flat_Surface", ref fltSurf);
            getOption.AddOptionToggle("Unexpected_Duplication", ref dupBrep);
            getOption.AddOptionToggle("Extruded_Curve_Wrong_Direction", ref extuCrv);
            getOption.AcceptNothing(true);

            // Option Setting Loop
            string[] subCommandsAry = new string[] {"On","On","On","On", "On"};
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
            Rhino.DocObjects.ObjRef[] objsRef_Arry = getObjects.Objects();
            List<Rhino.Geometry.Brep> breps_List = new List<Rhino.Geometry.Brep>();
            List<Rhino.DocObjects.RhinoObject> rhObjs_List = new List<Rhino.DocObjects.RhinoObject>();
            foreach (Rhino.DocObjects.ObjRef obj in objsRef_Arry)
            {
                breps_List.Add(obj.Brep());
                rhObjs_List.Add(obj.Object());
            }

            // Run subCommand dependently
            foreach (string subCommand in subCommandsAry)
            {
                if (subCommand == "On")
                {

                }
            }

            // Change the Color and Name
            // Iterate All rhObjs in List
            List<Rhino.DocObjects.RhinoObject> flatSrf_List = new List<Rhino.DocObjects.RhinoObject>();
            Rhino.Geometry.Collections.BrepFaceList brepFace_List;
            int index = 0;
            int counter = 0;
            foreach (Rhino.DocObjects.RhinoObject rhObj in rhObjs_List)
            {
                int j = 0;
                MM.SrfCollector(breps_List[index], out List<Rhino.Geometry.Surface> srf_List);
                foreach (Rhino.Geometry.Surface srf in srf_List)
                {
                    brepFace_List= breps_List[index].Faces;
                    MM.FlatSrfCheck(srf, modelTolerance, enlargeRatio, out bool flatSrfTrigger);
                    if (flatSrfTrigger)
                    {
                        flatSrf_List.Add(rhObj);
                        brepFace_List[j].PerFaceColor = System.Drawing.Color.Red;
                        counter++;
                        MM.ObjAttrRevise(rhObj, "NearlyFlatSurface|");

                    }
                    j++;
                }
                index++;
                
            }
            RhinoApp.WriteLine(counter.ToString());
            RhinoApp.WriteLine(modelTolerance.ToString());
            doc.Views.Redraw();

            return Rhino.Commands.Result.Success;
        }
    }
}
