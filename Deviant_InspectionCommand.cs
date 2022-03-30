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

            
            // Summary for using Accusation Name ///////////////////////////////////////////////////////////////////////
            Deviant_Inspector.Summary extrusion_Summary = new Summary(Accusation.Extrusion);
            Deviant_Inspector.Summary curl_Summary = new Summary(Accusation.Curl);
            Deviant_Inspector.Summary vertical_Summary = new Summary(Accusation.Vertical);
            Deviant_Inspector.Summary redundency_Summary = new Summary(Accusation.Redundency);

            // Set Options//////////////////////////////////////////////////////////////////////////////////////////////
            Rhino.Input.Custom.OptionToggle vertical_Toggle = new Rhino.Input.Custom.OptionToggle(true, "Off", "On");
            Rhino.Input.Custom.OptionToggle curl_Toggle = new Rhino.Input.Custom.OptionToggle(true, "Off", "On");
            Rhino.Input.Custom.OptionToggle extrusion_Toggle = new Rhino.Input.Custom.OptionToggle(true, "Off", "On");
            Rhino.Input.Custom.OptionToggle redundency_Toggle = new Rhino.Input.Custom.OptionToggle(false, "Off", "On");
            Rhino.Input.Custom.OptionToggle block_Toggle = new Rhino.Input.Custom.OptionToggle(true, "Exclude", "Include");

            // MM Instance Initiation //////////////////////////////////////////////////////////////////////////////////
            Deviant_Inspector.Method_Main mm = new Method_Main
            {
                ModelTolerance = doc.ModelAbsoluteTolerance,
                EnlargeRatio = 100,

                Curl_Toggle = curl_Toggle.CurrentValue,
                Vertical_Toggle = vertical_Toggle.CurrentValue,
                Extrusion_Toggle = extrusion_Toggle.CurrentValue,
                Redundency_Toggle = redundency_Toggle.CurrentValue
            };

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
            List<Rhino.Geometry.Brep> breps_List = new List<Rhino.Geometry.Brep>();
            List<Rhino.DocObjects.RhinoObject> brepObjs_List = new List<Rhino.DocObjects.RhinoObject>();
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
                    breps_List.Add(objRef.Brep());
                    brepObjs_List.Add(objRef.Object());
                    objRef_List.Add(objRef);
                }
            }

            // Summary Initiation //////////////////////////////////////////////////////////////////////////////////////
            int brepIssue_Count = 0;
            int faceIssue_Count = 0;
            int brep_Count = breps_List.Count;
            int face_Count = 0;

            // Change the Color and Name ///////////////////////////////////////////////////////////////////////////////
            // Iterate All brepObjs in List ////////////////////////////////////////////////////////////////////////////
            int i = 0;
            foreach (Rhino.Geometry.Brep brep in breps_List)
            {
                bool curlBrep_Result = false;
                bool verticalBrep_Result = false;
                bool redundencyBrep_Result = false;
                bool extrusionBrep_Result = false;
                List<int> facesCriminalIndex_List = new List<int>();

                if (brep != null)
                {
                    face_Count += brep.Faces.Count;

                    mm.Diagnose(brep,
                                out curlBrep_Result,
                                out verticalBrep_Result,
                                out redundencyBrep_Result,
                                out extrusionBrep_Result,
                                out int curlCriminalCount,
                                out int verticalCriminalCount,
                                out int extrusionCriminalCount,
                                out int redundencyCriminalCount,
                                out facesCriminalIndex_List);

                    curl_Summary.faceCriminalCount += curlCriminalCount;
                    vertical_Summary.faceCriminalCount += verticalCriminalCount;
                    extrusion_Summary.faceCriminalCount += extrusionCriminalCount;
                    redundency_Summary.faceCriminalCount += redundencyCriminalCount;
                }                

                // Name Revision ///////////////////////////////////////////////////////////////////////////////////////
                if (curlBrep_Result)
                {
                    mm.ObjNameRevise(brepObjs_List[i], curl_Summary.accusationObjName);
                    curl_Summary.brepCriminalCount++;
                }
                if (verticalBrep_Result)
                {
                    mm.ObjNameRevise(brepObjs_List[i], vertical_Summary.accusationObjName);
                    vertical_Summary.brepCriminalCount++;
                }
                if (extrusionBrep_Result)
                {
                    mm.ObjNameRevise(brepObjs_List[i], extrusion_Summary.accusationObjName);
                    extrusion_Summary.brepCriminalCount++;
                }
                if (redundencyBrep_Result)
                {
                    mm.ObjNameRevise(brepObjs_List[i], redundency_Summary.accusationObjName);
                    redundency_Summary.brepCriminalCount++;
                }
                // Commit Changes //////////////////////////////////////////////////////////////////////////////////////
                brepObjs_List[i].CommitChanges();
                
                // Color Change & Commit ///////////////////////////////////////////////////////////////////////////////
                if (facesCriminalIndex_List.Count != 0)
                {
                    brepIssue_Count++;
                    faceIssue_Count += facesCriminalIndex_List.Count;
                    mm.ObjColorRevise(color, brep, facesCriminalIndex_List, out Rhino.Geometry.Brep newBrep);
                    doc.Objects.Replace(objRef_List[i], newBrep);

                }

                i++;
            }

            // Block Iteration /////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            if (block_Toggle.CurrentValue)
            {
                foreach (Rhino.DocObjects.InstanceDefinition iDef in iDef_List)
                {
                    Rhino.DocObjects.RhinoObject[] rhObj_Array = iDef.GetObjects();
                    
                    List<Rhino.Geometry.GeometryBase> geobaseElse_List = new List<Rhino.Geometry.GeometryBase>();
                    
                    List<Rhino.Geometry.GeometryBase> brepInDef_List = new List<Rhino.Geometry.GeometryBase>();                    
                    List<Rhino.Geometry.GeometryBase> brepNew_List = new List<Rhino.Geometry.GeometryBase>();
                    List<Rhino.Geometry.GeometryBase> geobaseNew_List = new List<Rhino.Geometry.GeometryBase>();
                    
                    List<Rhino.DocObjects.ObjectAttributes> attrElse_List = new List<Rhino.DocObjects.ObjectAttributes>();
                    List<Rhino.DocObjects.ObjectAttributes> attrBrep_List = new List<Rhino.DocObjects.ObjectAttributes>();
                    List<Rhino.DocObjects.ObjectAttributes> attrNew_List = new List<Rhino.DocObjects.ObjectAttributes>();

                    // Instance Definition convert to Breps ////////////////////////////////////////////////////////////
                    foreach (Rhino.DocObjects.RhinoObject rhObj in rhObj_Array)
                    {
                        Rhino.DocObjects.ObjectAttributes objAttr = rhObj.Attributes;
                        Rhino.Geometry.GeometryBase gb = rhObj.Geometry;
                        if (gb.ObjectType == Rhino.DocObjects.ObjectType.Brep)
                        {
                            if (gb.HasBrepForm)
                            {
                                brepInDef_List.Add(Rhino.Geometry.Brep.TryConvertBrep(gb));
                                attrBrep_List.Add(objAttr);
                            }
                        }
                        else
                        {
                            geobaseElse_List.Add(gb);
                            attrElse_List.Add(objAttr);
                        }
                    }

                    // Summary /////////////////////////////////////////////////////////////////////////////////////////
                    if (brepInDef_List.Count != 0)
                    {
                        brep_Count += brepInDef_List.Count;
                    }

                    // Brep List Inspection ////////////////////////////////////////////////////////////////////////////
                    int brepIndex = 0;
                    foreach (Rhino.Geometry.Brep brep in brepInDef_List)
                    {
                        bool curlBrep_Result = false;
                        bool verticalBrep_Result = false;
                        bool redundencyBrep_Result = false;
                        bool extrusionBrep_Result = false;
                        List<int> facesCriminalIndex_List = new List<int>();

                        if (brep != null)
                        {
                            face_Count += brep.Faces.Count;

                            mm.Diagnose(brep,
                                        out curlBrep_Result,
                                        out verticalBrep_Result,
                                        out redundencyBrep_Result,
                                        out extrusionBrep_Result,
                                        out int curlCriminalCount,
                                        out int verticalCriminalCount,
                                        out int extrusionCriminalCount,
                                        out int redundencyCriminalCount,
                                        out facesCriminalIndex_List);

                            curl_Summary.faceCriminalCount += curlCriminalCount;
                            vertical_Summary.faceCriminalCount += verticalCriminalCount;
                            extrusion_Summary.faceCriminalCount += extrusionCriminalCount;
                            redundency_Summary.faceCriminalCount += redundencyCriminalCount;
                        }

                        if (facesCriminalIndex_List.Count != 0)
                        {
                            mm.ObjColorRevise(color, brep, facesCriminalIndex_List, out Rhino.Geometry.Brep newBrep);
                            brepNew_List.Add(newBrep);
                        }
                        else
                        {
                            brepNew_List.Add(brep);
                        }

                        brepIndex++;
                    }

                    geobaseNew_List.AddRange(brepNew_List);
                    geobaseNew_List.AddRange(geobaseElse_List);
                    attrNew_List.AddRange(attrBrep_List);
                    attrNew_List.AddRange(attrElse_List);


                    //doc.Replace IDef Objs ////////////////////////////////////////////////////////////////////////////
                    doc.InstanceDefinitions.ModifyGeometry(iDef.Index, geobaseNew_List, attrNew_List);
                    doc.Views.Redraw();
                }

            }
            else
            {
                RhinoApp.WriteLine("Blocks are not Inspected");
            }

            // Summary Dialog Information Collection ///////////////////////////////////////////////////////////////////
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

        // Issues: Check null Value



    }



}
