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
            getOption.AddOptionToggle("Vertical", ref abVerti);
            getOption.AddOptionToggle("Redundant", ref redunCP);
            getOption.AddOptionToggle("Curled", ref fltSurf);
            getOption.AddOptionToggle("Duplicated", ref dupBrep);
            getOption.AddOptionToggle("Extruded", ref extuCrv);
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
            int faceCount = 0;

            int brepFlatCount = 0;
            int faceFlatCount = 0;

            //int brepVertCount = 0;
            //int faceVertCount = 0;

            // Change the Color and Name
            // Iterate All rhObjs in List
            int i = 0;
            foreach (Rhino.Geometry.Brep brep in breps_List)
            {
                faceCount += brep.Faces.Count;

                List<int> faceFlatIndex_list = new List<int>();
                List<int> faceVertIndex_list = new List<int>();
                List<int> faceIssueIndex_List = new List<int>();
                bool run_Flat = false;
                //bool run_Vert = false;
                //bool run_Rend = false;
                //bool run_Dupl = false;
                //bool run_Extu = false;

                bool run_FlatBrep = false;
                bool run_VertBrep = false;
                bool run_RendBrep = false;
                bool run_DuplBrep = false;
                bool run_ExtuBrep = false;
                foreach (Rhino.Geometry.BrepFace brepFace in brep.Faces)
                {
                    // Flat Surface Iteration
                    if (fltSurf.CurrentValue)
                    {
                        run_Flat = MM.FlatSrfCheck(brepFace, modelTolerance, enlargeRatio);
                        if (run_Flat)
                        {
                            run_FlatBrep = true;
                            faceFlatCount++;
                            faceFlatIndex_list.Add(brepFace.FaceIndex);
                            if (!faceIssueIndex_List.Contains(brepFace.FaceIndex))
                            {
                                faceIssueIndex_List.Add(brepFace.FaceIndex);
                            }
                        }
                    }
                    
                }

                if (run_FlatBrep)
                {
                    MM.ObjNameRevise(rhObjs_List[i], "|NearlyFlatSurface|");
                    brepFlatCount++;
                }
                if (run_FlatBrep || 
                    run_VertBrep || 
                    run_RendBrep || 
                    run_DuplBrep || 
                    run_ExtuBrep)
                {
                    brepIssueCount++;
                    MM.ObjColorRevise(color, brep, faceIssueIndex_List, out Rhino.Geometry.Brep newBrep);
                    doc.Objects.Replace(objsRef_Arry[i], newBrep);
                }

                i++;
            }

            // Summary Dialog Information Collection
            string breakLine = "------------------------------------------------------ \n";
            string faceCount_String = "The Total Faces Selected Count: " + faceCount.ToString() + "\n";
            string brepCount_String = "The Total Breps Selected Count: " + brepCount.ToString() + "\n";
            string brepIssue_String = "Breps Have Deviant Components Count: " + brepIssueCount.ToString() + "\n";
            string faceFlat_String;
            string brepFlat_String;
            if (faceFlatCount != 0)
            {
                faceFlat_String = "Faces with 'Curled' Issue Count: " + faceFlatCount.ToString() + "\n";
                brepFlat_String = "Breps with 'Curled' Issue Count: " + brepFlatCount.ToString() + "\n";
            }
            else
            {
                faceFlat_String = "Faces with 'Curled' Issue Count: Not Inspected" + "\n";
                brepFlat_String = "Breps with 'Curled' Issue Count: Not Inspected" + "\n";
            }

            string dialogTitle = "Inspection Result";

            string dialogMessage = breakLine +
                                   faceCount_String +
                                   brepCount_String +
                                   breakLine +
                                   faceFlat_String +
                                   brepFlat_String +
                                   breakLine +
                                   brepIssue_String +
                                   "End of the Inspection";
            doc.Views.Redraw();
            doc.Objects.UnselectAll();
            Rhino.UI.Dialogs.ShowTextDialog(dialogMessage, dialogTitle);

            return Rhino.Commands.Result.Success;
        }
    }
}
