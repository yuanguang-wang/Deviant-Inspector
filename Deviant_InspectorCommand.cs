﻿using Rhino;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;


namespace Deviant_Inspector
{
    public class Deviant_InspectorCommand : Rhino.Commands.Command
    {

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
                EnlargeRatio = 100
            };

            // Set Options///////////////////////////////////////////////////////////////////////////////////////
            Rhino.Input.Custom.OptionToggle vertical_Toggle = new Rhino.Input.Custom.OptionToggle(true, "Off", "On");
            Rhino.Input.Custom.OptionToggle redundency_Toggle = new Rhino.Input.Custom.OptionToggle(true, "Off", "On");
            Rhino.Input.Custom.OptionToggle curl_Toggle = new Rhino.Input.Custom.OptionToggle(true, "Off", "On");
            Rhino.Input.Custom.OptionToggle extrusion_Toggle = new Rhino.Input.Custom.OptionToggle(true, "Off", "On");

            // Remarks on method AddOptionToggle
            // Body: str Must only consist of letters and numbers (no characters list periods, spaces, or dashes))
            // Type OptionToggle need a ref prefix
            getOption.AddOptionToggle("Vertical", ref vertical_Toggle);
            getOption.AddOptionToggle("Redundancy", ref redundency_Toggle);
            getOption.AddOptionToggle("Curl", ref curl_Toggle);
            getOption.AddOptionToggle("Extrusion", ref extrusion_Toggle);
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
            bool toggleAllValue = vertical_Toggle.CurrentValue   ||
                                  redundency_Toggle.CurrentValue ||
                                  curl_Toggle.CurrentValue       ||
                                  extrusion_Toggle.CurrentValue;
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
            bool result_Color = Rhino.UI.Dialogs.ShowColorDialog(ref color, true, "Select One Color to be Drawn on Deviants, Default is Red");
            if (result_Color == false)
            {
                RhinoApp.WriteLine("[COMMAND EXIT] Deviant Color is not Specified");
                return Rhino.Commands.Result.Failure;
            }
 
            // Totally Obj List with Brep % RhObj ///////////////////////////////////////////////////////////
            List<Rhino.Geometry.Brep> breps_List = new List<Rhino.Geometry.Brep>();
            List<Rhino.DocObjects.RhinoObject> rhObjs_List = new List<Rhino.DocObjects.RhinoObject>();
            foreach (Rhino.DocObjects.ObjRef objRef in objsRef_Arry)
            {
                breps_List.Add(objRef.Brep());
                rhObjs_List.Add(objRef.Object());
            }

            // Summary Initiation ///////////////////////////////////////////////////////////////////////////
            int brepIssue_Count = 0;
            int faceIssue_Count = 0;
            int brep_Count = breps_List.Count;
            int face_Count = 0;

            Deviant_Inspector.Summary extrusion_Summary = new Summary("Extrusion");
            Deviant_Inspector.Summary curl_Summary = new Summary("Curl");
            Deviant_Inspector.Summary vertical_Summary = new Summary("Vertical");
            Deviant_Inspector.Summary redundency_Summary = new Summary("Redundency");

            // Change the Color and Name ////////////////////////////////////////////////////////////////////
            // Iterate All rhObjs in List ///////////////////////////////////////////////////////////////////
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
