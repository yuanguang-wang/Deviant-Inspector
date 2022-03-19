using Rhino;
using System.Collections.Generic;
using System.Linq;

namespace Deviant_Inspector
{
    public class Deviant_InspectionCommand : Rhino.Commands.Command
    {
        public Deviant_InspectionCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static Deviant_InspectionCommand Instance { get; private set; }
        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "Devin";

        protected override Rhino.Commands.Result RunCommand(RhinoDoc doc, Rhino.Commands.RunMode mode)
        {
            /////////////////////////////////////////////////////////////////////////////////////////////////////
            // Initiation ///////////////////////////////////////////////////////////////////////////////////////
            Rhino.Input.Custom.GetObject getObjects = new Rhino.Input.Custom.GetObject
            {
                GeometryFilter = Rhino.DocObjects.ObjectType.Brep,
                GroupSelect = true,
                SubObjectSelect = false,
                DeselectAllBeforePostSelect = false
            };
            getObjects.EnableClearObjectsOnEntry(false);
            getObjects.EnableUnselectObjectsOnExit(false);

            // MM Instance Initiation ///////////////////////////////////////////////////////////////////////////
            Deviant_Inspector.Method_Main mm = new Method_Main
            {
                ModelTolerance = doc.ModelAbsoluteTolerance,
                EnlargeRatio = 100
            };

            // Summary for using Accusation Name ////////////////////////////////////////////////////////////
            Deviant_Inspector.Summary extrusion_Summary = new Summary(Accusation.Extrusion);
            Deviant_Inspector.Summary curl_Summary = new Summary(Accusation.Curl);
            Deviant_Inspector.Summary vertical_Summary = new Summary(Accusation.Vertical);
            Deviant_Inspector.Summary redundency_Summary = new Summary(Accusation.Redundency);

            /////////////////////////////////////////////////////////////////////////////////////////////////////
            // Set Options///////////////////////////////////////////////////////////////////////////////////////
            Rhino.Input.Custom.OptionToggle vertical_Toggle = new Rhino.Input.Custom.OptionToggle(true, "Off", "On");
            Rhino.Input.Custom.OptionToggle curl_Toggle = new Rhino.Input.Custom.OptionToggle(true, "Off", "On");
            Rhino.Input.Custom.OptionToggle extrusion_Toggle = new Rhino.Input.Custom.OptionToggle(true, "Off", "On");
            Rhino.Input.Custom.OptionToggle redundency_Toggle = new Rhino.Input.Custom.OptionToggle(false, "Off", "On");

            // Remarks on method AddOptionToggle
            // Body: str Must only consist of letters and numbers (no characters list periods, spaces, or dashes))
            // Type OptionToggle need a ref prefix
            getObjects.AddOptionToggle(Accusation.Vertical, ref vertical_Toggle);
            getObjects.AddOptionToggle(Accusation.Curl, ref curl_Toggle);
            getObjects.AddOptionToggle(Accusation.Extrusion, ref extrusion_Toggle);
            getObjects.AddOptionToggle(Accusation.Redundency, ref redundency_Toggle);

            // Option Setting Loop ////////////////////////////////////////////////////////////////////////
            bool havePreSelectedObjs = false;

            getObjects.SetCommandPrompt("Select the B-Reps to be Inspected");
            while (true)
            {
                Rhino.Input.GetResult getResult = getObjects.GetMultiple(1, 0);

                if (getResult == Rhino.Input.GetResult.Option)
                {
                    getObjects.EnablePreSelect(false, true);
                    continue;
                }
                else if (getResult == Rhino.Input.GetResult.Object) 
                {
                    getObjects.EnablePreSelect(true, true);
                    break;
                }
                else 
                {
                    RhinoApp.WriteLine("[COMMAND EXIT] Nothing is Selected to be Inspected");
                    doc.Views.Redraw();
                    return Rhino.Commands.Result.Cancel;
                }

            }

            //Unselected All Objs /////////////////////////////////////////////////////////////////////////
            if (havePreSelectedObjs)
            {
                for (int j = 0; j < getObjects.ObjectCount; j++)
                {
                    Rhino.DocObjects.RhinoObject rhinoObject = getObjects.Object(j).Object();
                    if (null != rhinoObject)
                        rhinoObject.Select(false);
                }
                doc.Views.Redraw();
            }

            // All Off Toggle Exception ///////////////////////////////////////////////////////////////////
            bool toggleAllValue = vertical_Toggle.CurrentValue   ||
                                  redundency_Toggle.CurrentValue ||
                                  curl_Toggle.CurrentValue       ||
                                  extrusion_Toggle.CurrentValue;
            if (!toggleAllValue)
            {
                RhinoApp.WriteLine("[COMMAND EXIT] All Inspection is Turned off, Nothing will be Inspected");
                doc.Views.Redraw();
                return Rhino.Commands.Result.Cancel;
            }

            // Brep Collection ////////////////////////////////////////////////////////////////////////////
            getObjects.SetCommandPrompt("Select the B-Reps to be Inspected");
            getObjects.GetMultiple(1,0);
            if (getObjects.CommandResult() != Rhino.Commands.Result.Success)
            {
                RhinoApp.WriteLine("[COMMAND EXIT] Nothing is Selected to be Inspected");
                doc.Views.Redraw();
                return Rhino.Commands.Result.Cancel;
            }
            Rhino.DocObjects.ObjRef[] objsRef_Arry = getObjects.Objects();
            doc.Objects.UnselectAll();

            // Color Set ///////////////////////////////////////////////////////////////////////////////////
            System.Drawing.Color color = System.Drawing.Color.Red;
            bool result_Color = Rhino.UI.Dialogs.ShowColorDialog(ref color, true, "Select One Color to be Drawn on Deviants, Default is Red");
            if (result_Color == false)
            {
                RhinoApp.WriteLine("[COMMAND EXIT] Deviant Color is not Specified");
                doc.Views.Redraw();
                return Rhino.Commands.Result.Cancel;
            }

            // Totally Obj List with Brep % RhObj //////////////////////////////////////////////////////////
            List<Rhino.Geometry.Brep> breps_List = new List<Rhino.Geometry.Brep>();
            List<Rhino.DocObjects.RhinoObject> rhObjs_List = new List<Rhino.DocObjects.RhinoObject>();
            foreach (Rhino.DocObjects.ObjRef objRef in objsRef_Arry)
            {
                breps_List.Add(objRef.Brep());
                rhObjs_List.Add(objRef.Object());
            }

            // Summary Initiation //////////////////////////////////////////////////////////////////////////
            int brepIssue_Count = 0;
            int faceIssue_Count = 0;
            int brep_Count = breps_List.Count;
            int face_Count = 0;

            // Change the Color and Name ///////////////////////////////////////////////////////////////////
            // Iterate All rhObjs in List //////////////////////////////////////////////////////////////////
            int i = 0;
            foreach (Rhino.Geometry.Brep brep in breps_List)
            {
                face_Count += brep.Faces.Count;
                List<int> facesCriminalIndex_List = new List<int>();

                bool curlBrep_Result = false;
                bool verticalBrep_Result = false;
                bool redundencyBrep_Result = false;
                bool extrusionBrep_Result = false;

                foreach (Rhino.Geometry.BrepFace brepFace in brep.Faces)
                {
                    // Flat Surface Iteration //////////////////////////
                    if (curl_Toggle.CurrentValue)
                    {
                        bool curlFace_Result = mm.CurlCheck(brepFace);
                        if (curlFace_Result)
                        {
                            curlBrep_Result = true;
                            curl_Summary.faceCriminalCount++;
                            facesCriminalIndex_List.Add(brepFace.FaceIndex);
                        }
                    }
                    // Vertical Surface Iteration //////////////////////
                    if (vertical_Toggle.CurrentValue)
                    {
                        bool verticalFace_Result = mm.VerticalCheck(brepFace);
                        if (verticalFace_Result)
                        {
                            verticalBrep_Result = true;
                            vertical_Summary.faceCriminalCount++;
                            facesCriminalIndex_List.Add(brepFace.FaceIndex);
                        }
                    }
                    // Extruded Surface Iteration //////////////////////
                    if (extrusion_Toggle.CurrentValue)
                    {
                        bool extrusionFace_Result = mm.ExtrusionCheck(brepFace);
                        if (extrusionFace_Result)
                        {
                            extrusionBrep_Result = true;
                            extrusion_Summary.faceCriminalCount++;
                            facesCriminalIndex_List.Add(brepFace.FaceIndex);
                        }
                    }
                    // Extruded Surface Iteration //////////////////////
                    if (redundency_Toggle.CurrentValue)
                    {
                        bool redundencyFace_Result = mm.RedundencyCheck(brepFace);
                        if (redundencyFace_Result)
                        {
                            redundencyBrep_Result = true;
                            redundency_Summary.faceCriminalCount++;
                            facesCriminalIndex_List.Add(brepFace.FaceIndex);
                        }
                    }

                }
                // Name Revision ///////////////////////////////////////
                if (curlBrep_Result)
                {
                    mm.ObjNameRevise(rhObjs_List[i], curl_Summary.accusationObjName);
                    curl_Summary.brepCriminalCount++;
                }
                if (verticalBrep_Result)
                {
                    mm.ObjNameRevise(rhObjs_List[i], vertical_Summary.accusationObjName);
                    vertical_Summary.brepCriminalCount++;
                }
                if (extrusionBrep_Result)
                {
                    mm.ObjNameRevise(rhObjs_List[i], extrusion_Summary.accusationObjName);
                    extrusion_Summary.brepCriminalCount++;
                }
                if (redundencyBrep_Result)
                {
                    mm.ObjNameRevise(rhObjs_List[i], redundency_Summary.accusationObjName);
                    redundency_Summary.brepCriminalCount++;
                }
                // Commit Changes ///////////////////////////////////////
                rhObjs_List[i].CommitChanges();
                // Color Change & Commit ////////////////////////////////
                if (curlBrep_Result       || 
                    verticalBrep_Result   || 
                    redundencyBrep_Result || 
                    extrusionBrep_Result
                   )
                {
                    facesCriminalIndex_List = facesCriminalIndex_List.Distinct().ToList();
                    brepIssue_Count++;
                    faceIssue_Count += facesCriminalIndex_List.Count;
                    mm.ObjColorRevise(color, brep, facesCriminalIndex_List, out Rhino.Geometry.Brep newBrep);
                    doc.Objects.Replace(objsRef_Arry[i], newBrep);

                }

                i++;
            }

            // Summary Dialog Information Collection ////////////////////////////////////////////////////////
            string breakLine = "------------------------------------------------------ \n";
            string faceCount_String = "The Total Faces Selected Count: " + face_Count.ToString() + "\n";
            string brepCount_String = "The Total Breps Selected Count: " + brep_Count.ToString() + "\n";
            string brepIssue_String = "Breps Have Deviant Components Count: " + brepIssue_Count.ToString() + "\n";
            string faceIssue_String = "Faces Have Deviant Components Count: " + faceIssue_Count.ToString() + "\n";

            // Dialog Set
            string dialogTitle = "Inspection Result";
            string dialogMessage = breakLine +
                                   faceCount_String +
                                   brepCount_String +
                                   
                                   curl_Summary.InspectionResult(curl_Toggle.CurrentValue) +
                                   vertical_Summary.InspectionResult(vertical_Toggle.CurrentValue) +
                                   extrusion_Summary.InspectionResult(extrusion_Toggle.CurrentValue) +
                                   redundency_Summary.InspectionResult(redundency_Toggle.CurrentValue) +

                                   breakLine +
                                   faceIssue_String +
                                   brepIssue_String +
                                   
                                   breakLine +
                                   "End of the Inspection\n";

            doc.Views.Redraw();
            Rhino.UI.Dialogs.ShowTextDialog(dialogMessage, dialogTitle);
        
            return Rhino.Commands.Result.Success;
        }



    }



}
