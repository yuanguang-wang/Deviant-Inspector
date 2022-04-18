using Rhino;
using System.Collections.Generic;
using System.Linq;

namespace Deviant_Inspector
{
    /// <summary>
    /// main method here
    /// </summary>

    internal class MethodAssembly
    {
        public delegate bool FaceDiagnoseDel(Rhino.Geometry.BrepFace bFace);

        public delegate bool ObjNameChangeDel(Rhino.DocObjects.RhinoObject rhObj, string accusation);

        public delegate bool ObjColorChangeDel(Rhino.Geometry.Brep brep,
                                                List<int> criminalIndex_List,
                                            out Rhino.Geometry.Brep newBrep);
    }

    internal static class Core
    {
        #region ATTR

        public static System.Drawing.Color Color { get; set; }
        public static double ModelTolerance { get; set; }
        public static int EnlargeRatio = 100;

        #endregion ATTR

        #region MTHD

        public static bool ObjNameRevise(Rhino.DocObjects.RhinoObject rhObj, string accusation)
        {
            //Name Revision ////////////////////////////////////////////
            if (rhObj.Attributes.Name == null)
            {
                rhObj.Attributes.Name = "_";
            }
            else if (!rhObj.Attributes.Name.Contains("_"))
            {
                rhObj.Attributes.Name += "_";
            }

            string currentName = rhObj.Attributes.Name;
            if (!currentName.Contains(accusation))
            {
                currentName += accusation;
            }
            rhObj.Attributes.Name = currentName;

            return true;
        }

        public static bool ObjNameRollback(Rhino.DocObjects.RhinoObject rhObj, string accusation)
        {
            if (rhObj.Attributes.Name == null)
            {
                rhObj.Attributes.Name = "_";
            }
            string currentName = rhObj.Attributes.Name;
            if (currentName.Contains(accusation))
            {
                currentName = currentName.Replace(accusation, "");
            }
            if (currentName.Last() == '_')
            {
                currentName = currentName.Remove(currentName.Length - 1);
            }
            rhObj.Attributes.Name = currentName;

            return true;
        }

        public static bool ObjColorRevise(Rhino.Geometry.Brep brep,
                                          List<int> criminalIndex_List,
                                      out Rhino.Geometry.Brep newBrep)
        {
            newBrep = brep.DuplicateBrep();
            foreach (int i in criminalIndex_List)
            {
                newBrep.Faces[i].PerFaceColor = Color;
            }

            return true;
        }

        public static bool ObjColorRollback(Rhino.Geometry.Brep brep,
                                            List<int> criminalIndex_List,
                                        out Rhino.Geometry.Brep newBrep)
        {
            newBrep = brep.DuplicateBrep();
            foreach (int i in criminalIndex_List)
            {
                newBrep.Faces[i].PerFaceColor = System.Drawing.Color.Empty;
            }

            return true;
        }

        public static bool CurlCheck(Rhino.Geometry.BrepFace bFace)
        {
            double relaviteTolerance = ModelTolerance * EnlargeRatio;
            if (bFace.IsPlanar(ModelTolerance) == false)
            {
                if (bFace.IsPlanar(relaviteTolerance) == true)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static bool VerticalCheck(Rhino.Geometry.BrepFace bFace)
        {
            double relaviteTolerance = ModelTolerance * EnlargeRatio;
            Rhino.Geometry.Curve curve;
            Rhino.Geometry.Curve curve_0 = bFace.IsoCurve(0, 0);
            if (curve_0.IsLinear())
            {
                curve = curve_0;
            }
            else
            {
                Rhino.Geometry.Curve curve_1 = bFace.IsoCurve(1, 0);
                if (curve_1.IsLinear())
                {
                    curve = curve_1;
                }
                else
                {
                    return false;
                    // Face is not an Extrusion
                }
            }
            double distanceX = System.Math.Abs(curve.PointAtStart.X - curve.PointAtEnd.X);
            double distanceY = System.Math.Abs(curve.PointAtStart.Y - curve.PointAtEnd.Y);
            if (ModelTolerance > distanceX)
            {
                if (ModelTolerance > distanceY)
                {
                    return false;
                    // This is a Vertical Extrusion
                }
                else if (relaviteTolerance > distanceY)
                {
                    return true;
                    // This is an Issue
                }
                else
                {
                    return false;
                    // This is an Intended Diagnal Extrusion
                }
            }
            else if (relaviteTolerance > distanceX)
            {
                return true;
            }
            else
            {
                if (ModelTolerance > distanceY)
                {
                    return false;
                    // This is an Intended Diagnal Extrusion
                }
                else if (relaviteTolerance > distanceY)
                {
                    return true;
                    // This is an Issue
                }
                else
                {
                    return false;
                    // This is an Intended Diagnal Extrusion
                }
            }
        }

        public static bool ExtrusionCheck(Rhino.Geometry.BrepFace bFace)
        {
            // Avoiding Bad Objects ////////////////////////////////////////////////////////
            Rhino.Geometry.BrepLoop loop = bFace.OuterLoop;
            Rhino.Geometry.Curve[] segs;

            if (loop != null)
            {
                segs = loop.To3dCurve().DuplicateSegments();
            }
            else
            {
                return false;
            }

            if (segs.Length <= 2)
            {
                return false;
            }

            // Point of the Outer Loop Collection //////////////////////////////////////////
            double modelToleranceSquare = ModelTolerance * ModelTolerance;
            List<Rhino.Geometry.Point3d> pt_List = new List<Rhino.Geometry.Point3d>();
            foreach (Rhino.Geometry.Curve segment in segs)
            {
                if (!segment.IsLinear())
                {
                    return false;
                }

                pt_List.Add(segment.PointAtEnd);
            }

            // Projection Test /////////////////////////////////////////////////////////////
            Rhino.Geometry.Line baseLine = new Rhino.Geometry.Line(pt_List[0], pt_List[1]);
            foreach (Rhino.Geometry.Point3d pt in pt_List)
            {
                Rhino.Geometry.Point3d ptProjected = baseLine.ClosestPoint(pt, false);
                double distance = pt.DistanceToSquared(ptProjected);
                if (distance > modelToleranceSquare)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool RedundancyCheck(Rhino.Geometry.BrepFace bFace)
        {
            Rhino.Geometry.Curve loop = bFace.OuterLoop.To3dCurve();

            // Itirate each Loop Segment ///////////////////////////////////////////////////
            var crvSimplified = loop.Simplify(Rhino.Geometry.CurveSimplifyOptions.All, ModelTolerance, 1);
            if (crvSimplified != null)
            {
                bool crvDuplicationDetect = Rhino.Geometry.GeometryBase.GeometryEquals(crvSimplified, loop);
                if (crvDuplicationDetect == false)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool Dispatch(Rhino.DocObjects.ObjRef[] objsRef_Arry,
                                    out List<Rhino.DocObjects.InstanceDefinition> iDef_List,
                                    out List<Rhino.DocObjects.ObjRef> objRef_List)
        {
            iDef_List = new List<Rhino.DocObjects.InstanceDefinition>();
            objRef_List = new List<Rhino.DocObjects.ObjRef>();

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

            return true;
        }

        #endregion MTHD
    }

    internal class Diagnose
    {
        /// <summary>
        /// Need to create objs which count is equal to the diagnose core number
        /// as well as the option toggle number.
        /// Current Count: 4
        /// </summary>

        #region ATTR

        public readonly string On = "On";
        public readonly string Off = "Off";
        public int FaceCriminalCount { get; set; }
        public int BrepCriminalCount { get; set; }
        public List<int> FacesCriminalIndex_List { get; set; }
        public string Accusation { get; set; }
        public string AccusationObjName { get; set; }
        public bool BrepCriminalCheckResult { get; set; }
        public Rhino.Input.Custom.OptionToggle Option_Toggle { get; set; }
        public MethodAssembly.FaceDiagnoseDel CoreMethodHandler { get; set; }
        public bool OptionToggleDefaultValue { get; set; }

        #endregion ATTR

        #region CTOR

        public Diagnose(string accusation,
                        MethodAssembly.FaceDiagnoseDel faceDiagnoseMethod,
                        bool pasedOptionToggleDefaultValue)
        {
            this.Accusation = accusation;
            this.AccusationObjName = "[" + accusation + "]";
            this.CoreMethodHandler = faceDiagnoseMethod;
            this.OptionToggleDefaultValue = pasedOptionToggleDefaultValue;
            this.FaceCriminalCount = 0;
            this.BrepCriminalCount = 0;
            this.FacesCriminalIndex_List = new List<int>();
            this.BrepCriminalCheckResult = false;
            this.Option_Toggle = new Rhino.Input.Custom.OptionToggle(this.OptionToggleDefaultValue, this.Off, this.On);
        }

        #endregion CTOR

        #region MTHD

        public bool FaceDiagnose(Rhino.Geometry.BrepFace bFace)
        {
            if (this.Option_Toggle.CurrentValue)
            {
                if (CoreMethodHandler(bFace)) // Return the faceCheckResult //
                {
                    this.BrepCriminalCheckResult = true;
                    this.FaceCriminalCount += 1;
                    this.FacesCriminalIndex_List.Add(bFace.FaceIndex);
                }
            }
            return this.BrepCriminalCheckResult;
        }

        public string InspectionResult()
        {
            string faceCriminal_String;
            string brepCriminal_String;
            string summary_String;
            if (this.Option_Toggle.CurrentValue)
            {
                faceCriminal_String = "Faces with '" + Accusation + "' Issue Count: " + FaceCriminalCount.ToString() + "\n";
                brepCriminal_String = "Breps with '" + Accusation + "' Issue Count: " + BrepCriminalCount.ToString() + "\n";
                summary_String = Summary.breakLine +
                                 faceCriminal_String +
                                 brepCriminal_String;
            }
            else
            {
                summary_String = "";
            }
            return summary_String;
        }

        #endregion MTHD
    }

    internal class Inspection
    {
        #region ATTR

        public Rhino.RhinoDoc CurrentDoc { get; set; }
        public List<Diagnose> DiagnoseObjs_List { get; set; }
        public bool BlockInspectionToggle { get; set; }
        public string CmdNameHolder { get; set; }
        public MethodAssembly.ObjColorChangeDel ObjColorChangeHandler { get; set; }
        public MethodAssembly.ObjNameChangeDel ObjNameChangeHandler { get; set; }

        #endregion ATTR

        #region CTOR

        public Inspection(Rhino.RhinoDoc currentDoc,
                          List<Diagnose> diagnoseObjs_List,
                          string passedCmdName)
        {
            this.CurrentDoc = currentDoc;
            this.DiagnoseObjs_List = diagnoseObjs_List;
            if (passedCmdName == CmdName.Inspection)
            {
                this.ObjColorChangeHandler = new MethodAssembly.ObjColorChangeDel(Core.ObjColorRevise);
                this.ObjNameChangeHandler = new MethodAssembly.ObjNameChangeDel(Core.ObjNameRevise);
            }
            else if (passedCmdName == CmdName.Rollback)
            {
                this.ObjColorChangeHandler = new MethodAssembly.ObjColorChangeDel(Core.ObjColorRollback);
                this.ObjNameChangeHandler = new MethodAssembly.ObjNameChangeDel(Core.ObjNameRollback);
            }
            else
            {
            }
            this.CmdNameHolder = passedCmdName;
        }

        #endregion CTOR

        #region MTHD

        /// <summary>
        /// Run In Main Program
        /// </summary>

        public bool Selector(out Rhino.DocObjects.ObjRef[] objsRef_Arry)
        {
            objsRef_Arry = null;
            ///<remarks> Initiation </remarks>
            Rhino.Input.Custom.GetObject getObjects = new Rhino.Input.Custom.GetObject
            {
                GeometryFilter = Rhino.DocObjects.ObjectType.Brep | Rhino.DocObjects.ObjectType.InstanceReference,
                GroupSelect = true,
                SubObjectSelect = false,
                DeselectAllBeforePostSelect = false
            };
            getObjects.EnableClearObjectsOnEntry(false);
            getObjects.EnableUnselectObjectsOnExit(false);

            Rhino.Input.Custom.OptionToggle block_Toggle = new Rhino.Input.Custom.OptionToggle(false, "Exclude", "Include");

            ///<remarks>
            /// Body: str Must only consist of letters and numbers
            /// no characters list periods, spaces, or dashes
            ///</remarks>
            foreach (Diagnose diagnoseObj in DiagnoseObjs_List)
            {
                Rhino.Input.Custom.OptionToggle tempToggle = diagnoseObj.Option_Toggle;
                getObjects.AddOptionToggle(diagnoseObj.Accusation, ref tempToggle);
                diagnoseObj.Option_Toggle = tempToggle;
            }
            getObjects.AddOptionToggle("Blocks", ref block_Toggle);

            this.CurrentDoc.Objects.UnselectAll();
            this.CurrentDoc.Views.Redraw();

            getObjects.SetCommandPrompt("Select the breps to run " + this.CmdNameHolder + " command");
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
                    RhinoApp.WriteLine("Brep selection finished");
                    getObjects.EnablePreSelect(true, true);
                    break;
                }
                else
                {
                    RhinoApp.WriteLine("[COMMAND EXIT] Nothing is selected to run " + this.CmdNameHolder + " command");
                    CurrentDoc.Views.Redraw();
                    return false;
                }
            }
            this.BlockInspectionToggle = block_Toggle.CurrentValue;

            bool toggleAllValue = false;
            foreach (Diagnose diagnoseObj in DiagnoseObjs_List)
            {
                toggleAllValue = toggleAllValue || diagnoseObj.Option_Toggle.CurrentValue;
            }
            if (toggleAllValue == false)
            {
                RhinoApp.WriteLine("[COMMAND EXIT] All toggles are turned off, no command will run.");
                this.CurrentDoc.Views.Redraw();
                return false;
            }

            ///<remarks> Brep Collection </remarks>
            if (getObjects.CommandResult() != Rhino.Commands.Result.Success)
            {
                RhinoApp.WriteLine("[COMMAND EXIT] Nothing is selected to run " + this.CmdNameHolder + " command");
                this.CurrentDoc.Views.Redraw();
                return false;
            }
            objsRef_Arry = getObjects.Objects();
            this.CurrentDoc.Objects.UnselectAll();

            if (objsRef_Arry == null)
            {
                return false;
            }
            return true;
        }

        public bool BrepFilter(Rhino.DocObjects.InstanceDefinition iDef,
                           out List<Rhino.DocObjects.RhinoObject> brepObjs_List,
                           out List<Rhino.DocObjects.RhinoObject> otherObjs_List)
        {
            Rhino.DocObjects.RhinoObject[] rhObjs_Array = iDef.GetObjects();
            brepObjs_List = new List<Rhino.DocObjects.RhinoObject>();
            otherObjs_List = new List<Rhino.DocObjects.RhinoObject>();

            foreach (Rhino.DocObjects.RhinoObject rhObj in rhObjs_Array)
            {
                Rhino.Geometry.GeometryBase gb = rhObj.Geometry;
                if (gb.ObjectType == Rhino.DocObjects.ObjectType.Brep)
                {
                    if (gb.HasBrepForm)
                    {
                        brepObjs_List.Add(rhObj);
                    }
                }
                else
                {
                    otherObjs_List.Add(rhObj);
                }
            }
            return true;
        }

        public bool IDefDiagnoseLoop(Rhino.DocObjects.InstanceDefinition iDef)
        {
            List<Rhino.DocObjects.ObjectAttributes> attrBrep_List = new List<Rhino.DocObjects.ObjectAttributes>();
            List<Rhino.DocObjects.ObjectAttributes> attrOther_List = new List<Rhino.DocObjects.ObjectAttributes>();

            List<Rhino.Geometry.GeometryBase> geoBaseOthers_List = new List<Rhino.Geometry.GeometryBase>();
            List<Rhino.Geometry.Brep> breps_List = new List<Rhino.Geometry.Brep>();

            this.BrepFilter(iDef,
                            out List<Rhino.DocObjects.RhinoObject> brepObjs_List,
                            out List<Rhino.DocObjects.RhinoObject> otherObjs_List);
            foreach (Rhino.DocObjects.RhinoObject brepObj in brepObjs_List)
            {
                this.BrepDiagnoseLoop(brepObj, ref breps_List);
                attrBrep_List.Add(brepObj.Attributes);
            }
            foreach (Rhino.DocObjects.RhinoObject otherObj in otherObjs_List)
            {
                attrOther_List.Add(otherObj.Attributes);
                geoBaseOthers_List.Add(otherObj.Geometry);
            }

            List<Rhino.DocObjects.ObjectAttributes> attributes_List = new List<Rhino.DocObjects.ObjectAttributes>();
            List<Rhino.Geometry.GeometryBase> geometryBases_List = new List<Rhino.Geometry.GeometryBase>();

            attributes_List.AddRange(attrBrep_List);
            attributes_List.AddRange(attrOther_List);
            geometryBases_List.AddRange(breps_List);
            geometryBases_List.AddRange(geoBaseOthers_List);

            CurrentDoc.InstanceDefinitions.ModifyGeometry(iDef.Index, geometryBases_List, attributes_List);
            CurrentDoc.Views.Redraw();

            return true;
        }

        public bool BrepDiagnoseLoop(Rhino.DocObjects.ObjRef objRef)
        {
            Rhino.Geometry.Brep brep = objRef.Brep();
            Rhino.DocObjects.RhinoObject brepObj = objRef.Object();

            this.DiagnoseLoopTemplate(brep, out List<int> combinedFacesCriminalIndex_List);
            this.BrepColorChangeLoop(combinedFacesCriminalIndex_List, objRef, brep);
            this.BrepNameChangeLoop(brepObj);

            return true;
        }

        public bool BrepDiagnoseLoop(Rhino.DocObjects.RhinoObject brepObj, ref List<Rhino.Geometry.Brep> brepReplaced_List)
        {
            Rhino.Geometry.Brep brep = brepObj.Geometry as Rhino.Geometry.Brep;
            this.DiagnoseLoopTemplate(brep, out List<int> combinedFacesCriminalIndex_List);
            this.BrepColorChangeLoop(combinedFacesCriminalIndex_List, ref brepReplaced_List, brep);
            this.BrepNameChangeLoop(brepObj);
            return true;
        }

        public bool DiagnoseLoopTemplate(Rhino.Geometry.Brep brep, out List<int> combinedFacesCriminalIndex_List)
        {
            combinedFacesCriminalIndex_List = new List<int>();
            if (brep != null)
            {
                Summary.Face_Count += brep.Faces.Count;
                Summary.Brep_Count++;

                // Face Check Though Delegate //////////////////////////////////////////////////////
                foreach (Rhino.Geometry.BrepFace bFace in brep.Faces)
                {
                    if (bFace != null)
                    {
                        foreach (Diagnose diagnoseObj in DiagnoseObjs_List)
                        {
                            diagnoseObj.FaceDiagnose(bFace);
                        }
                    }
                }
                // Cascading list //////////////////////////////////////////////////////////////////
                foreach (Diagnose diagnoseObj in DiagnoseObjs_List)
                {
                    combinedFacesCriminalIndex_List.AddRange(diagnoseObj.FacesCriminalIndex_List);
                    // Clean the list for next diagnose /////////////////
                    diagnoseObj.FacesCriminalIndex_List = new List<int>();
                }
                combinedFacesCriminalIndex_List = combinedFacesCriminalIndex_List.Distinct().ToList();
            }

            return true;
        }

        public bool BrepNameChangeLoop(Rhino.DocObjects.RhinoObject brepObj)
        {
            // Revise the Name /////////////////////////////////////////////////////////////////////
            foreach (Diagnose diagnoseObj in DiagnoseObjs_List)
            {
                if (diagnoseObj.BrepCriminalCheckResult)
                {
                    this.ObjNameChangeHandler(brepObj, diagnoseObj.AccusationObjName);
                    diagnoseObj.BrepCriminalCount += 1;
                    diagnoseObj.BrepCriminalCheckResult = false;
                }
            }
            brepObj.CommitChanges();
            return true;
        }

        public bool BrepColorChangeLoop(List<int> combinedFacesCriminalIndex_List,
                                        Rhino.DocObjects.ObjRef objRef,
                                        Rhino.Geometry.Brep brep)
        {
            // Revise the Color /////////////////////////////////////////////////////////////////////
            if (combinedFacesCriminalIndex_List.Count != 0)
            {
                Summary.BrepIssue_Count++;
                Summary.FaceIssue_Count += combinedFacesCriminalIndex_List.Count;
                this.ObjColorChangeHandler(brep, combinedFacesCriminalIndex_List, out Rhino.Geometry.Brep newBrep);
                this.CurrentDoc.Objects.Replace(objRef, newBrep);
            }
            return true;
        }

        public bool BrepColorChangeLoop(List<int> combinedFacesCriminalIndex_List,
                                    ref List<Rhino.Geometry.Brep> brepReplaced_List,
                                        Rhino.Geometry.Brep brep)
        {
            if (combinedFacesCriminalIndex_List.Count != 0)
            {
                Summary.BrepIssue_Count++;
                Summary.FaceIssue_Count += combinedFacesCriminalIndex_List.Count;
                this.ObjColorChangeHandler(brep, combinedFacesCriminalIndex_List, out Rhino.Geometry.Brep newBrep);
                brepReplaced_List.Add(newBrep);
            }
            else
            {
                brepReplaced_List.Add(brep);
            }
            return true;
        }

        public bool InspectionResult()
        {
            string inspectionResult = "";
            foreach (Diagnose diagnoseObj in DiagnoseObjs_List)
            {
                inspectionResult += diagnoseObj.InspectionResult();
            }
            Summary.InspectionResult = inspectionResult;
            return true;
        }

        #endregion MTHD
    }

    internal static class Summary
    {
        #region ATTR

        public const string breakLine = "------------------------------------------------------ \n";
        public const string dialogTitle = "Inspection Result";
        public static int Face_Count { get; set; }
        public static int Brep_Count { get; set; }
        public static int FaceIssue_Count { get; set; }
        public static int BrepIssue_Count { get; set; }
        public static string DialogMessage { get; set; }
        public static string FaceCount_String { get; set; }
        public static string BrepCount_String { get; set; }
        public static string BrepIssue_String { get; set; }
        public static string FaceIssue_String { get; set; }
        public static string InspectionResult { get; set; }

        #endregion ATTR

        #region MTHD

        public static bool Result()
        {
            FaceCount_String = "The Total Faces Selected Count: " + Face_Count.ToString() + "\n";
            BrepCount_String = "The Total Breps Selected Count: " + Brep_Count.ToString() + "\n";
            BrepIssue_String = "Breps Have Deviant Components Count: " + BrepIssue_Count.ToString() + "\n";
            FaceIssue_String = "Faces Have Deviant Components Count: " + FaceIssue_Count.ToString() + "\n";

            DialogMessage = breakLine +
                               FaceCount_String +
                               BrepCount_String +

                               InspectionResult +

                               breakLine +
                               FaceIssue_String +
                               BrepIssue_String +

                               breakLine +
                               "End of the Inspection\n";

            Rhino.UI.Dialogs.ShowTextDialog(DialogMessage, dialogTitle);
            return true;
        }

        #endregion MTHD
    }

    internal static class Accusation
    {
        public static readonly string Curl = "Curl";
        public static readonly string Extrusion = "Extrusion";
        public static readonly string Vertical = "Vertical";
        public static readonly string Redundancy = "Redundancy";
    }

    internal static class CmdName
    {
        public static readonly string Inspection = "Inspection";
        public static readonly string Rollback = "Rollback";
    }
}