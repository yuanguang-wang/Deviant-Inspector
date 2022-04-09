using Rhino;
using System.Collections.Generic;
using System.Linq;

namespace Deviant_Inspector
{
    public class Deviant_InspectionCommand : Rhino.Commands.Command
    {
        public Deviant_InspectionCommand()
        {        
            Instance = this;
        }

        public static Deviant_InspectionCommand Instance { get; private set; }

        public override string EnglishName => "Devin";

        protected override Rhino.Commands.Result RunCommand(RhinoDoc doc, Rhino.Commands.RunMode mode)
        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Initiation //////////////////////////////////////////////////////////////////////////////////////////////
            Rhino.Input.Custom.GetObject getObjects = new Rhino.Input.Custom.GetObject
            {
                GeometryFilter = Rhino.DocObjects.ObjectType.Brep | Rhino.DocObjects.ObjectType.InstanceReference,
                GroupSelect = true,
                SubObjectSelect = false,
                DeselectAllBeforePostSelect = false
            };
            getObjects.EnableClearObjectsOnEntry(false);
            getObjects.EnableUnselectObjectsOnExit(false);

            // Set Options//////////////////////////////////////////////////////////////////////////////////////////////
            Rhino.Input.Custom.OptionToggle vertical_Toggle = new Rhino.Input.Custom.OptionToggle(true, "Off", "On");
            Rhino.Input.Custom.OptionToggle curl_Toggle = new Rhino.Input.Custom.OptionToggle(true, "Off", "On");
            Rhino.Input.Custom.OptionToggle extrusion_Toggle = new Rhino.Input.Custom.OptionToggle(true, "Off", "On");
            Rhino.Input.Custom.OptionToggle redundency_Toggle = new Rhino.Input.Custom.OptionToggle(false, "Off", "On");
            Rhino.Input.Custom.OptionToggle block_Toggle = new Rhino.Input.Custom.OptionToggle(false, "Exclude", "Include");

            // Remarks on method AddOptionToggle ///////////////////////////////////////////////////////////////////////
            // Body: str Must only consist of letters and numbers (no characters list periods, spaces, or dashes)///////
            // Type OptionToggle need a ref prefix /////////////////////////////////////////////////////////////////////
            getObjects.AddOptionToggle(Accusation.Vertical, ref vertical_Toggle);
            getObjects.AddOptionToggle(Accusation.Curl, ref curl_Toggle);
            getObjects.AddOptionToggle(Accusation.Extrusion, ref extrusion_Toggle);
            getObjects.AddOptionToggle(Accusation.Redundency, ref redundency_Toggle);
            getObjects.AddOptionToggle("Blocks", ref block_Toggle);
            // Unselect All Objs before Inspection
            doc.Objects.UnselectAll();
            doc.Views.Redraw();

            // Option Setting Loop /////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            
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
                    RhinoApp.WriteLine("Brep Selection Finished; " +
                                       "Select One Color to be Drawn on Deviants, " +
                                       "Default is Red");
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

            // All Off Toggle Exception ////////////////////////////////////////////////////////////////////////////////
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

            // Brep Collection /////////////////////////////////////////////////////////////////////////////////////////
            if (getObjects.CommandResult() != Rhino.Commands.Result.Success)
            {
                RhinoApp.WriteLine("[COMMAND EXIT] Nothing is Selected to be Inspected");
                doc.Views.Redraw();
                return Rhino.Commands.Result.Cancel;
            }
            Rhino.DocObjects.ObjRef[] objsRef_Arry = getObjects.Objects();
            doc.Objects.UnselectAll();

            // Color Set ///////////////////////////////////////////////////////////////////////////////////////////////
            System.Drawing.Color color = System.Drawing.Color.Red;
            bool result_Color = Rhino.UI.Dialogs.ShowColorDialog(ref color, 
                                                                 true, 
                                                                "Select One Color to be Drawn on Deviants, " +
                                                                "Default is Red");
            if (result_Color == false)
            {
                RhinoApp.WriteLine("[COMMAND EXIT] Deviant Color is not Specified");
                doc.Views.Redraw();
                return Rhino.Commands.Result.Cancel;
            }

            // Totally Obj List with Brep % RhObj //////////////////////////////////////////////////////////////////////
            List<Rhino.DocObjects.InstanceDefinition> iDef_List = new List<Rhino.DocObjects.InstanceDefinition>();
            List<Rhino.DocObjects.ObjRef> objRef_List = new List<Rhino.DocObjects.ObjRef>();

            // Dispatch IRef and Brep //////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            
            foreach (Rhino.DocObjects.ObjRef objRef in objsRef_Arry)
            {
                if (objRef.Object() is Rhino.DocObjects.InstanceObject iRefObj)
                {
                    Rhino.DocObjects.InstanceDefinition iDef = iRefObj.InstanceDefinition;
                    if (!iDef_List.Contains(iDef))
                    {
                        iDef_List.Add(iDef);
                    }
                }
                else
                {
                    objRef_List.Add(objRef);
                }
            }


            // Change the Color and Name ///////////////////////////////////////////////////////////////////////////////
            // Iterate All brepObjs in List ////////////////////////////////////////////////////////////////////////////
            Deviant_Inspector.Diagnose curl_Diagnose = new Diagnose(Accusation.Curl, Core.CurlCheck, curl_Toggle.CurrentValue);
            Deviant_Inspector.Diagnose vertical_Diagnose = new Diagnose(Accusation.Vertical, Core.VerticalCheck, vertical_Toggle.CurrentValue);
            Deviant_Inspector.Diagnose extrusion_Diagnose = new Diagnose(Accusation.Extrusion, Core.ExtrusionCheck, extrusion_Toggle.CurrentValue);
            Deviant_Inspector.Diagnose redundency_Diagnose = new Diagnose(Accusation.Redundency, Core.RedundencyCheck, redundency_Toggle.CurrentValue);

            List<Diagnose> diagnoseObjs_List = new List<Diagnose>
            {
                curl_Diagnose,
                vertical_Diagnose,
                extrusion_Diagnose,
                redundency_Diagnose
            };

            Deviant_Inspector.Inspection inspector = new Inspection(doc, diagnoseObjs_List, color);


            foreach (Rhino.DocObjects.ObjRef objRef in objRef_List)
            {
                inspector.DiagnoseLoop(objRef);
            }

            inspector.InspectionResult();

            #region Block
            // Block Iteration /////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //if (block_Toggle.CurrentValue)
            //{
            //    foreach (Rhino.DocObjects.InstanceDefinition iDef in iDef_List)
            //    {
            //        Rhino.DocObjects.RhinoObject[] rhObj_Array = iDef.GetObjects();

            //        List<Rhino.Geometry.GeometryBase> geobaseElse_List = new List<Rhino.Geometry.GeometryBase>();

            //        List<Rhino.Geometry.GeometryBase> brepInDef_List = new List<Rhino.Geometry.GeometryBase>();                    
            //        List<Rhino.Geometry.GeometryBase> brepNew_List = new List<Rhino.Geometry.GeometryBase>();
            //        List<Rhino.Geometry.GeometryBase> geobaseNew_List = new List<Rhino.Geometry.GeometryBase>();

            //        List<Rhino.DocObjects.ObjectAttributes> attrElse_List = new List<Rhino.DocObjects.ObjectAttributes>();
            //        List<Rhino.DocObjects.ObjectAttributes> attrBrep_List = new List<Rhino.DocObjects.ObjectAttributes>();
            //        List<Rhino.DocObjects.ObjectAttributes> attrNew_List = new List<Rhino.DocObjects.ObjectAttributes>();

            //        List<Rhino.DocObjects.RhinoObject> brepObjectsInDef_List = new List<Rhino.DocObjects.RhinoObject>();

            //        // Instance Definition convert to Breps ////////////////////////////////////////////////////////////
            //        foreach (Rhino.DocObjects.RhinoObject rhObj in rhObj_Array)
            //        {
            //            Rhino.DocObjects.ObjectAttributes objAttr = rhObj.Attributes;
            //            Rhino.Geometry.GeometryBase gb = rhObj.Geometry;
            //            if (gb.ObjectType == Rhino.DocObjects.ObjectType.Brep)
            //            {
            //                if (gb.HasBrepForm)
            //                {
            //                    brepInDef_List.Add(Rhino.Geometry.Brep.TryConvertBrep(gb));
            //                    attrBrep_List.Add(objAttr);
            //                    brepObjectsInDef_List.Add(rhObj);
            //                }
            //            }
            //            else
            //            {
            //                geobaseElse_List.Add(gb);
            //                attrElse_List.Add(objAttr);
            //            }
            //        }

            //        // Summary /////////////////////////////////////////////////////////////////////////////////////////
            //        if (brepInDef_List.Count != 0)
            //        {
            //            //brep_Count += brepInDef_List.Count;
            //        }

            //        // Brep List Inspection ////////////////////////////////////////////////////////////////////////////
            //        int brepIndex = 0;
            //        foreach (Rhino.Geometry.Brep brep in brepInDef_List)
            //        {
            //            bool curlBrep_Result = false;
            //            bool verticalBrep_Result = false;
            //            bool redundencyBrep_Result = false;
            //            bool extrusionBrep_Result = false;
            //            List<int> facesCriminalIndex_List = new List<int>();

            //            if (brep != null)
            //            {
            //                face_Count += brep.Faces.Count;

            //                inspector.Diagnose(brep,
            //                            out curlBrep_Result,
            //                            out verticalBrep_Result,
            //                            out redundencyBrep_Result,
            //                            out extrusionBrep_Result,
            //                            out int curlCriminalCount,
            //                            out int verticalCriminalCount,
            //                            out int extrusionCriminalCount,
            //                            out int redundencyCriminalCount,
            //                            out facesCriminalIndex_List);

            //                curl_Summary.faceCriminalCount += curlCriminalCount;
            //                vertical_Summary.faceCriminalCount += verticalCriminalCount;
            //                extrusion_Summary.faceCriminalCount += extrusionCriminalCount;
            //                redundency_Summary.faceCriminalCount += redundencyCriminalCount;
            //            }

            //            // Block Name Revision Loop ////////////////////////////////////////////////////////////////////
            //            if (curlBrep_Result)
            //            {
            //                inspector.ObjNameRevise(brepObjectsInDef_List[brepIndex], curl_Summary.accusationObjName);
            //                curl_Summary.brepCriminalCount++;
            //            }
            //            if (verticalBrep_Result)
            //            {
            //                inspector.ObjNameRevise(brepObjectsInDef_List[brepIndex], vertical_Summary.accusationObjName);
            //                vertical_Summary.brepCriminalCount++;
            //            }
            //            if (extrusionBrep_Result)
            //            {
            //                inspector.ObjNameRevise(brepObjectsInDef_List[brepIndex], extrusion_Summary.accusationObjName);
            //                extrusion_Summary.brepCriminalCount++;
            //            }
            //            if (redundencyBrep_Result)
            //            {
            //                inspector.ObjNameRevise(brepObjectsInDef_List[brepIndex], redundency_Summary.accusationObjName);
            //                redundency_Summary.brepCriminalCount++;
            //            }

            //            brepObjectsInDef_List[brepIndex].CommitChanges();

            //            // Block Color Revision Loop ///////////////////////////////////////////////////////////////////
            //            if (facesCriminalIndex_List.Count != 0)
            //            {
            //                brepIssue_Count++;
            //                faceIssue_Count += facesCriminalIndex_List.Count;
            //                inspector.ObjColorRevise(color, brep, facesCriminalIndex_List, out Rhino.Geometry.Brep newBrep);
            //                brepNew_List.Add(newBrep);
            //            }
            //            else
            //            {
            //                brepNew_List.Add(brep);
            //            }


            //            brepIndex++;
            //        }

            //        geobaseNew_List.AddRange(brepNew_List);
            //        geobaseNew_List.AddRange(geobaseElse_List);
            //        attrNew_List.AddRange(attrBrep_List);
            //        attrNew_List.AddRange(attrElse_List);


            //        //doc.Replace IDef Objs ////////////////////////////////////////////////////////////////////////////
            //        doc.InstanceDefinitions.ModifyGeometry(iDef.Index, geobaseNew_List, attrNew_List);
            //        doc.Views.Redraw();
            //    }

            //}
            //else
            //{
            //    RhinoApp.WriteLine("Blocks are not Inspected");
            //}
            #endregion

            // Summary Dialog Information Collection ///////////////////////////////////////////////////////////////////
            doc.Views.Redraw();
            Summary.Result();
            


            return Rhino.Commands.Result.Success;
        }

        


    }



}
