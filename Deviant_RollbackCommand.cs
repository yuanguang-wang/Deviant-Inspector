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

        public override string EnglishName => "Devro";

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

            getObjects.SetCommandPrompt("Select the B-Reps to be Rolled Back");
            while (true)
            {                
                Rhino.Input.GetResult getResult = getObjects.GetMultiple(1, 0);

                if (getResult == Rhino.Input.GetResult.Option)
                {
                    getObjects.EnablePreSelect(false, true);
                    continue;
                }
                else if (getResult != Rhino.Input.GetResult.Object)
                {
                    RhinoApp.WriteLine("[COMMAND EXIT] Nothing is Selected to be Rolled Back");
                    doc.Views.Redraw();
                    return Rhino.Commands.Result.Cancel;
                }

                if (getObjects.ObjectsWerePreselected)
                {
                    havePreSelectedObjs = true;
                    getObjects.EnablePreSelect(false, true);
                    continue;
                }

                break;
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
            bool toggleAllValue = vertical_Toggle.CurrentValue ||
                                  redundency_Toggle.CurrentValue ||
                                  curl_Toggle.CurrentValue ||
                                  extrusion_Toggle.CurrentValue;
            if (!toggleAllValue)
            {
                RhinoApp.WriteLine("[COMMAND EXIT] All Rollback is Turned off, Nothing will be Rolled Back");
                doc.Views.Redraw();
                return Rhino.Commands.Result.Cancel;
            }

            // Brep Collection ////////////////////////////////////////////////////////////////////////////
            if (getObjects.CommandResult() != Rhino.Commands.Result.Success)
            {
                RhinoApp.WriteLine("[COMMAND EXIT] Rollback Command has been Interrupted");
                doc.Views.Redraw();
                return Rhino.Commands.Result.Cancel;
            }
            else
            {
                RhinoApp.WriteLine("Rollback Setting Finished, Deviants will be Released Now");
                System.Threading.Thread.Sleep(1000);
            }
            Rhino.DocObjects.ObjRef[] objsRef_Arry = getObjects.Objects();

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
            bool rollback_Result = false;

            foreach (Rhino.Geometry.Brep brep in breps_List)
            {
                List<int> facesCriminalIndex_List = new List<int>();
                foreach (Rhino.Geometry.BrepFace brepFace in brep.Faces)
                {
                    // Curl Surface Iteration ///////////////////////
                    if (curl_Toggle.CurrentValue)
                    {
                        bool curlFace_Result = mm.CurlCheck(brepFace);
                        if (curlFace_Result)
                        {
                            curl_Summary.faceCriminalCount++;
                            facesCriminalIndex_List.Add(brepFace.FaceIndex);
                        }
                    }
                    // Vertical Surface Iteration ///////////////////
                    if (vertical_Toggle.CurrentValue)
                    {
                        bool verticalFace_Result = mm.VerticalCheck(brepFace);
                        if (verticalFace_Result)
                        {
                            vertical_Summary.faceCriminalCount++;
                            facesCriminalIndex_List.Add(brepFace.FaceIndex);
                        }
                    }
                    // Extruded Surface Iteration ///////////////////
                    if (extrusion_Toggle.CurrentValue)
                    {
                        bool extrusionFace_Result = mm.ExtrusionCheck(brepFace);
                        if (extrusionFace_Result)
                        {
                            extrusion_Summary.faceCriminalCount++;
                            facesCriminalIndex_List.Add(brepFace.FaceIndex);
                        }
                    }
                    // Extruded Surface Iteration ////////////////////
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
                    rollback_Result = mm.ObjNameRollback(rhObjs_List[i], curl_Summary.accusationObjName);
                }
                if (vertical_Toggle.CurrentValue)
                {
                    rollback_Result = mm.ObjNameRollback(rhObjs_List[i], vertical_Summary.accusationObjName);
                }
                if (extrusion_Toggle.CurrentValue)
                {
                    rollback_Result = mm.ObjNameRollback(rhObjs_List[i], extrusion_Summary.accusationObjName);
                }
                if (redundency_Toggle.CurrentValue)
                {
                    rollback_Result = mm.ObjNameRollback(rhObjs_List[i], redundency_Summary.accusationObjName);
                }
                rhObjs_List[i].CommitChanges();

                // i++ //////////////////////////////////////////////////
                i++;
            }

            if (rollback_Result)
            {
                RhinoApp.WriteLine("Rollback Finished");
            }

            doc.Views.Redraw();
            return Rhino.Commands.Result.Success;
        }
    }
}