﻿using System;
using Rhino;
using System.Collections.Generic;
using System.Linq;

namespace Deviant_Inspector
{
    public class Deviant_RollbackCommand : Rhino.Commands.Command
    {
        public Deviant_RollbackCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static Deviant_RollbackCommand Instance { get; private set; }

        public override string EnglishName => "Deviantrollback";

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

            // MM Instance Initiation ///////////////////////////////////////////////////////////////////////////
            Deviant_Inspector.Method_Main mm = new Method_Main
            {
                ModelTolerance = doc.ModelAbsoluteTolerance,
                EnlargeRatio = 100
            };

            // Summary for using Accusation Name ////////////////////////////////////////////////////////////
            Deviant_Inspector.Summary extrusion_Summary = new Summary("Extrusion");
            Deviant_Inspector.Summary curl_Summary = new Summary("Curl");
            Deviant_Inspector.Summary vertical_Summary = new Summary("Vertical");
            Deviant_Inspector.Summary redundency_Summary = new Summary("Redundency");

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
            getOption.SetCommandPrompt("Select the Deviant Type to be Rollback");
            while (true)
            {
                Rhino.Input.GetResult rc = getOption.Get();
                if (getOption.CommandResult() != Rhino.Commands.Result.Success)
                {
                    return getOption.CommandResult();
                }

                if (rc == Rhino.Input.GetResult.Nothing)
                {
                    RhinoApp.WriteLine("Rollback Setting Finished");
                    break;
                }
                else if (rc == Rhino.Input.GetResult.Option)
                {
                    continue;
                }
            }

            // All Off Toggle Exception ///////////////////////////////////////////////////////////////////
            bool toggleAllValue = vertical_Toggle.CurrentValue ||
                                  redundency_Toggle.CurrentValue ||
                                  curl_Toggle.CurrentValue ||
                                  extrusion_Toggle.CurrentValue;
            if (!toggleAllValue)
            {
                RhinoApp.WriteLine("[COMMAND EXIT] All Rollback is Turned off, Nothing will be Rolled Back");
                return Rhino.Commands.Result.Failure;
            }

            // Brep Collection ////////////////////////////////////////////////////////////////////////////
            getObjects.SetCommandPrompt("Select the B-Reps to be Rolled Back");
            getObjects.GetMultiple(1, 0);
            if (getObjects.CommandResult() != Rhino.Commands.Result.Success)
            {
                RhinoApp.WriteLine("[COMMAND EXIT] Nothing is Selected to be Rolled Back");
                return Rhino.Commands.Result.Failure;
            }
            Rhino.DocObjects.ObjRef[] objsRef_Arry = getObjects.Objects();
            doc.Objects.UnselectAll();

            // Totally Obj List with Brep % RhObj ///////////////////////////////////////////////////////////
            List<Rhino.Geometry.Brep> breps_List = new List<Rhino.Geometry.Brep>();
            List<Rhino.DocObjects.RhinoObject> rhObjs_List = new List<Rhino.DocObjects.RhinoObject>();
            foreach (Rhino.DocObjects.ObjRef objRef in objsRef_Arry)
            {
                breps_List.Add(objRef.Brep());
                rhObjs_List.Add(objRef.Object());
            }

            // Iterate Every rhObj to Rollback Deviants //////////////////////////////////////////////////////
            int i = 0;
            foreach (Rhino.Geometry.Brep brep in breps_List)
            {
                List<int> facesCriminalIndex_List = new List<int>();
                foreach (Rhino.Geometry.BrepFace brepFace in brep.Faces)
                {
                    // Curl Surface Iteration //////////////////////
                    if (curl_Toggle.CurrentValue)
                    {
                        bool curlFace_Result = mm.CurlCheck(brepFace);
                        if (curlFace_Result)
                        {
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
                            redundency_Summary.faceCriminalCount++;
                            facesCriminalIndex_List.Add(brepFace.FaceIndex);
                        }
                    }

                    if (facesCriminalIndex_List.Count != 0)
                    {
                        facesCriminalIndex_List = facesCriminalIndex_List.Distinct().ToList();
                        mm.ObjColorRollback(brep, facesCriminalIndex_List, out Rhino.Geometry.Brep newBrep);
                        doc.Objects.Replace(objsRef_Arry[i], newBrep);
                    }
                }
                // Name Rollback ////////////////////////////////////////
                if (curl_Toggle.CurrentValue)
                {
                    mm.ObjNameRollback(rhObjs_List[i], curl_Summary.accusationObjName);
                }
                if (vertical_Toggle.CurrentValue)
                {
                    mm.ObjNameRollback(rhObjs_List[i], vertical_Summary.accusationObjName);
                }
                if (extrusion_Toggle.CurrentValue)
                {
                    mm.ObjNameRollback(rhObjs_List[i], extrusion_Summary.accusationObjName);
                }
                if (redundency_Toggle.CurrentValue)
                {
                    mm.ObjNameRollback(rhObjs_List[i], redundency_Summary.accusationObjName);
                }

                i++;
            }

            doc.Views.Redraw();
            return Rhino.Commands.Result.Success;
        }
    }
}