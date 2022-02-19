using Rhino;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

using MM = Deviant_Inspector.Method_Main;

namespace Deviant_Inspector
{
    public class Deviant_InspectorCommand : Rhino.Commands.Command
    {

        /// <summary>
        /// TO-DO List
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
            System.Drawing.Color color = System.Drawing.Color.Red;

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
            getOption.SetCommandPrompt("Select the Inspector to be Excuted");
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
                    continue;
                }
            }

            // All Off Toggle Exception
            bool toggleAllValue = abVerti.CurrentValue ||
                                  redunCP.CurrentValue ||
                                  fltSurf.CurrentValue ||
                                  dupBrep.CurrentValue ||
                                  extuCrv.CurrentValue;
            if (!toggleAllValue)
            {
                RhinoApp.WriteLine("[COMMAND EXIT] All Inspection is Turned off, Nothing will be Inspected");
                return Rhino.Commands.Result.Failure;
            }

            // Brep Collection
            getObjects.SetCommandPrompt("Select the B-Reps to be Inspected");
            getObjects.GetMultiple(1,0);
            if (getObjects.CommandResult() != Rhino.Commands.Result.Success)
            {
                RhinoApp.WriteLine("[COMMAND EXIT] Nothing is Selected to be Inspected");
                return Rhino.Commands.Result.Failure;
            }
            Rhino.DocObjects.ObjRef[] objsRef_Arry = getObjects.Objects();

            // Color Set
            bool run_Color = true;
            run_Color = Rhino.UI.Dialogs.ShowColorDialog(ref color, true, "Select One Color to be Drawn on Deviants, Default is Red");
            if (run_Color == false)
            {
                RhinoApp.WriteLine("[COMMAND EXIT] Deviant Color is not Specified");
                return Rhino.Commands.Result.Failure;
            }

            // Totally Obj List with Brep % RhObj
            List<Rhino.Geometry.Brep> breps_List = new List<Rhino.Geometry.Brep>();
            List<Rhino.DocObjects.RhinoObject> rhObjs_List = new List<Rhino.DocObjects.RhinoObject>();
            foreach (Rhino.DocObjects.ObjRef obj in objsRef_Arry)
            {
                breps_List.Add(obj.Brep());
                rhObjs_List.Add(obj.Object());
            }

            // Summary Variable Set
            int brepIssueCount = 0;
            int brepCount = breps_List.Count;

            int brepFlatCount = 0;
            int faceFlatCount = 0;

            //int brepVertCount = 0;
            //int faceVertCount = 0;

            // Change the Color and Name
            // Iterate All rhObjs in List
            int i = 0;
            foreach (Rhino.Geometry.Brep brep in breps_List)
            {
                List<int> faceFlatIndex_list = new List<int>();
                List<int> faceVertIndex_list = new List<int>();
                List<int> faceIssueIndex_List = new List<int>();
                bool run_Flat = false;
                bool run_Vert = false;
                bool run_Rend = false;
                bool run_Dupl = false;
                bool run_Extu = false;
                foreach (Rhino.Geometry.BrepFace brepFace in brep.Faces)
                {
                    if (fltSurf.CurrentValue)
                    {
                        run_Flat = MM.FlatSrfCheck(brepFace, modelTolerance, enlargeRatio, out bool trigger_FlatSrf);
                        if (trigger_FlatSrf)
                        {
                            faceFlatCount++;
                            faceFlatIndex_list.Add(brepFace.FaceIndex);
                            if (!faceIssueIndex_List.Contains(brepFace.FaceIndex))
                            {
                                faceIssueIndex_List.Add(brepFace.FaceIndex);
                            }
                        }
                    }
                    
                }

                if (run_Flat)
                {
                    MM.ObjNameRevise(rhObjs_List[i], "|NearlyFlatSurface|");
                    brepFlatCount++;
                }
                if (run_Flat || run_Vert || run_Rend || run_Dupl || run_Extu)
                {
                    MM.ObjColorRevise(color, brep, faceIssueIndex_List, out Rhino.Geometry.Brep newBrep);
                    doc.Objects.Replace(objsRef_Arry[i], newBrep);
                    brepIssueCount++;
                }
                i++;
            }

            // Summary Dialog Information Collection
            string breakLine = "------------------------------------------------------ \n";
            double brepPercent = brepIssueCount / brepCount;
            string faceFlat_String;
            string brepFlat_String;
            if (faceFlatCount != 0)
            {
                faceFlat_String = "Faces with 'Nearly Flat Surface' Issue Count: " + faceFlatCount.ToString() + "\n";
                brepFlat_String = "Breps with 'Nearly Flat Surface' Issue Count: " + brepFlatCount.ToString() + "\n";
            }
            else
            {
                faceFlat_String = "Faces with 'Nearly Flat Surface' Issue Count: Not Inspected" + "\n";
                brepFlat_String = "Breps with 'Nearly Flat Surface' Issue Count: Not Inspected" + "\n";
            }
            string brepIssuePercentage_String = "issue brep percentage is " + brepPercent.ToString() + "%\n";

            string dialogTitle = "Inspection Result";
            
            string dialogMessage = breakLine +
                                   faceFlat_String +
                                   brepFlat_String +
                                   breakLine +
                                   brepIssuePercentage_String;
            doc.Views.Redraw();
            Rhino.UI.Dialogs.ShowTextDialog(dialogMessage, dialogTitle);

            return Rhino.Commands.Result.Success;
        }
    }
}
