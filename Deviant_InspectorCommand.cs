using Rhino;
using System;
using System.Collections.Generic;
using System.Windows.Forms;


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
            // Initiation ///////////////////////////////////////////////////////////////////////////////////////
            Rhino.Input.Custom.GetOption getOption = new Rhino.Input.Custom.GetOption();
            Rhino.Input.Custom.GetObject getObjects = new Rhino.Input.Custom.GetObject 
            {
                GeometryFilter = Rhino.DocObjects.ObjectType.Brep,
                GroupSelect = true,
                SubObjectSelect = false
            };

            System.Drawing.Color color = System.Drawing.Color.Red;

            // MM Instance Initiation ///////////////////////////////////////////////////////////////////////////
            Deviant_Inspector.Method_Main mm = new Method_Main
            {
                ModelTolerance = doc.ModelAbsoluteTolerance,
                EnlargeRatio = 100,
                Color = color
            };

            // Set Options///////////////////////////////////////////////////////////////////////////////////////
            Rhino.Input.Custom.OptionToggle abVerti = new Rhino.Input.Custom.OptionToggle(true, "Off", "On");
            Rhino.Input.Custom.OptionToggle redunCP = new Rhino.Input.Custom.OptionToggle(true, "Off", "On");
            Rhino.Input.Custom.OptionToggle fltSurf = new Rhino.Input.Custom.OptionToggle(true, "Off", "On");
            Rhino.Input.Custom.OptionToggle dupBrep = new Rhino.Input.Custom.OptionToggle(true, "Off", "On");
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

            // Option Setting Loop ////////////////////////////////////////////////////////////////////////
            getOption.SetCommandPrompt("Select the Inspection to be Excuted");
            while (true)
            {
                Rhino.Input.GetResult rc = getOption.Get();
                if (getOption.CommandResult() != Rhino.Commands.Result.Success)
                {
                return getOption.CommandResult();
                }

                if (rc == Rhino.Input.GetResult.Nothing)
                {
                    RhinoApp.WriteLine("Inspection Setting Finished");
                    break;
                }
                else if (rc == Rhino.Input.GetResult.Option)
                {
                    continue;
                }
            }

            // All Off Toggle Exception ///////////////////////////////////////////////////////////////////
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

            // Brep Collection ////////////////////////////////////////////////////////////////////////////
            getObjects.SetCommandPrompt("Select the B-Reps to be Inspected");
            getObjects.GetMultiple(1,0);
            if (getObjects.CommandResult() != Rhino.Commands.Result.Success)
            {
                RhinoApp.WriteLine("[COMMAND EXIT] Nothing is Selected to be Inspected");
                return Rhino.Commands.Result.Failure;
            }
            Rhino.DocObjects.ObjRef[] objsRef_Arry = getObjects.Objects();
            doc.Objects.UnselectAll();

            // Color Set ///////////////////////////////////////////////////////////////////////////////////
            bool result_Color = true;
            result_Color = Rhino.UI.Dialogs.ShowColorDialog(ref color, true, "Select One Color to be Drawn on Deviants, Default is Red");
            if (result_Color == false)
            {
                RhinoApp.WriteLine("[COMMAND EXIT] Deviant Color is not Specified");
                return Rhino.Commands.Result.Failure;
            }

            // Totally Obj List with Brep % RhObj ///////////////////////////////////////////////////////////
            List<Rhino.Geometry.Brep> breps_List = new List<Rhino.Geometry.Brep>();
            List<Rhino.DocObjects.RhinoObject> rhObjs_List = new List<Rhino.DocObjects.RhinoObject>();
            foreach (Rhino.DocObjects.ObjRef obj in objsRef_Arry)
            {
                breps_List.Add(obj.Brep());
                rhObjs_List.Add(obj.Object());
            }

            // Summary Variable Set /////////////////////////////////////////////////////////////////////////
            int brepIssueCount = 0;
            int brepCount = breps_List.Count;
            int faceCount = 0;

            int brepFlatCount = 0;
            int faceFlatCount = 0;

            int brepVertCount = 0;
            int faceVertCount = 0;

            // Change the Color and Name ////////////////////////////////////////////////////////////////////
            // Iterate All rhObjs in List ///////////////////////////////////////////////////////////////////
            int i = 0;
            foreach (Rhino.Geometry.Brep brep in breps_List)
            {
                faceCount += brep.Faces.Count;

                List<int> facesIssueIndex_List = new List<int>();

                bool result_FlatBrep = false;
                bool result_VertBrep = false;
                bool result_RendBrep = false;
                bool result_DuplBrep = false;
                bool result_ExtuBrep = false;
                foreach (Rhino.Geometry.BrepFace brepFace in brep.Faces)
                {
                    // Flat Surface Iteration //////////////////////////
                    if (fltSurf.CurrentValue)
                    {
                        bool result_FlatFace = mm.FlatSrfCheck(brepFace);
                        if (result_FlatFace)
                        {
                            result_FlatBrep = true;
                            faceFlatCount++;
                            if (!facesIssueIndex_List.Contains(brepFace.FaceIndex))
                            {
                                facesIssueIndex_List.Add(brepFace.FaceIndex);
                            }
                        }
                    }
                    // Vertical Surface Iteration //////////////////////
                    if (abVerti.CurrentValue)
                    {
                        bool result_VertFace = mm.VerticalCheck(brepFace);
                        if (result_VertFace)
                        {
                            result_VertBrep = true;
                            faceVertCount++;
                            if (!facesIssueIndex_List.Contains(brepFace.FaceIndex))
                            {
                                facesIssueIndex_List.Add(brepFace.FaceIndex);
                            }
                        }
                    }
                    // Extruded Surface Iteration //////////////////////
                    if (extuCrv.CurrentValue)
                    {
                        bool result_ExtuFace = mm.ExtrudeCheck(brepFace);
                        if (result_ExtuFace)
                        {
                            result_ExtuBrep = true;

                            if (!facesIssueIndex_List.Contains(brepFace.FaceIndex))
                            {
                                facesIssueIndex_List.Add(brepFace.FaceIndex);
                            }
                        }
                    }
                    // New Iteration Below /////////////////////////////////

                }

                if (result_FlatBrep)
                {
                    mm.ObjNameRevise(rhObjs_List[i], "|Curled|");
                    brepFlatCount++;
                }
                if (result_VertBrep)
                {
                    mm.ObjNameRevise(rhObjs_List[i], "|Vertical|");
                    brepVertCount++;
                }
                if (result_ExtuBrep)
                {
                    mm.ObjNameRevise(rhObjs_List[i], "|Extruded|");
                    brepVertCount++; //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                }
                if (result_FlatBrep || 
                    result_VertBrep || 
                    result_RendBrep || 
                    result_DuplBrep || 
                    result_ExtuBrep
                    )
                {
                    brepIssueCount++;
                    mm.ObjColorRevise(brep, facesIssueIndex_List, out Rhino.Geometry.Brep newBrep);
                    doc.Objects.Replace(objsRef_Arry[i], newBrep);
                }

                i++;
            }

            // Summary Dialog Information Collection
            string breakLine = "------------------------------------------------------ \n";
            string faceCount_String = "The Total Faces Selected Count: " + faceCount.ToString() + "\n";
            string brepCount_String = "The Total Breps Selected Count: " + brepCount.ToString() + "\n";
            string brepIssue_String = "Breps Have Deviant Components Count: " + brepIssueCount.ToString() + "\n";
            
            // Flat String Set
            string faceFlat_String;
            string brepFlat_String;
            if (faceFlatCount != 0)
            {
                faceFlat_String = "Faces with 'Curled' Issue Count: " + faceFlatCount.ToString() + "\n";
                brepFlat_String = "Breps with 'Curled' Issue Count: " + brepFlatCount.ToString() + "\n";
            }
            else
            {
                faceFlat_String = "Faces with 'Curled' Issue Count: 0" + "\n";
                brepFlat_String = "Breps with 'Curled' Issue Count: 0" + "\n";
            }

            // Vertical String Set
            string faceVert_String;
            string brepVert_String;
            if (faceVertCount != 0)
            {
                faceVert_String = "Faces with 'Vertical' Issue Count: " + faceVertCount.ToString() + "\n";
                brepVert_String = "Breps with 'Vertical' Issue Count: " + brepVertCount.ToString() + "\n";
            }
            else
            {
                faceVert_String = "Faces with 'Vertical' Issue Count: 0" + "\n";
                brepVert_String = "Breps with 'Vertical' Issue Count: 0" + "\n";
            }

            // New String Set

            // Dialog Set
            string dialogTitle = "Inspection Result";
            string dialogMessage = breakLine +
                                   faceCount_String +
                                   brepCount_String +
                                   breakLine +
                                   faceFlat_String +
                                   brepFlat_String +
                                   breakLine +
                                   faceVert_String +
                                   brepVert_String +
                                   breakLine +
                                   brepIssue_String +
                                   "End of the Inspection\n" +
                                   breakLine +
                                   "[NOTE] Numbers Report 0 means:\n" +
                                   "       1. Inspection related didn't result.\n" +
                                   "       2. No issue found.\n";
            doc.Views.Redraw();
            Rhino.UI.Dialogs.ShowTextDialog(dialogMessage, dialogTitle);
        
            return Rhino.Commands.Result.Success;
        }


        public string Summary(string accusation)
        {
            //string breakLine = "------------------------------------------------------ \n";
            //int faceCriminalCount = 0;
            //int brepCriminalCOunt = 0;

            return "test";
        }

    }
}
