using System;
using System.Collections.Generic;
using System.Linq;
using Rhino;



namespace Deviant_Inspector
{
    /// <summary>
    /// main method here
    /// </summary> 

    class Method_Main { }

    public delegate bool FaceDiagnoseDel(Rhino.Geometry.BrepFace bFace);
    
    static class Core
    {

        #region ATTR
        public static double ModelTolerance { get; set; }
        public static int EnlargeRatio { get; set; }
        #endregion

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

        public static bool ObjColorRevise(System.Drawing.Color color, 
                                   Rhino.Geometry.Brep brep, 
                                   List<int> criminalIndex_List, 
                               out Rhino.Geometry.Brep newBrep)
        {
            newBrep = brep.DuplicateBrep();
            foreach (int i in criminalIndex_List)
            {
                newBrep.Faces[i].PerFaceColor = color;
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

        public static bool RedundencyCheck(Rhino.Geometry.BrepFace bFace) 
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

        #endregion
    }

    class Diagnose
    {
        /// <summary>
        /// Need to create objs which count is equal to the diagnose core number
        /// as well as the option toggle number.
        /// Current Count: 4
        /// </summary>
        
        #region ATTR 
        public int FaceCriminalCount { get; set; }
        public int BrepCriminalCount { get; set; }
        public List<int> FacesCriminalIndex_List { get; set; }
        public string Accusation { get; set; }
        public string AccusationObjName { get; set; }
        public bool BrepCriminalCheckResult { get; set; }
        public bool Option_Toggle { get; set; }
        public FaceDiagnoseDel CoreMethodHandler { get; set; }

        #endregion

        #region CTOR
        public Diagnose(string accusation, FaceDiagnoseDel faceDiagnoseMethod, bool optionToggle)
        {
            this.Accusation = accusation;
            this.AccusationObjName = "[" + accusation + "]";
            this.CoreMethodHandler = faceDiagnoseMethod;
            this.Option_Toggle = optionToggle;
            this.FaceCriminalCount = 0;
            this.BrepCriminalCount = 0;
            this.FacesCriminalIndex_List = new List<int>();
            this.BrepCriminalCheckResult = false;
        }
        #endregion

        #region MTHD
        public bool FaceDiagnose(Rhino.Geometry.BrepFace bFace)
        {            
            if (Option_Toggle)
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
            if (this.Option_Toggle)
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

        #endregion
    }

    class Inspection
    {

        #region ATTR
        public Rhino.RhinoDoc CurrentDoc { get; set; }
        public System.Drawing.Color Color { get; set; }
        public List<Diagnose> DiagnoseObjs_List { get; set; }
        #endregion

        #region CTOR
        public Inspection(Rhino.RhinoDoc doc, List<Diagnose> diagnoseObjs_List, System.Drawing.Color color)
        {
            this.CurrentDoc = doc;
            Core.ModelTolerance = doc.ModelAbsoluteTolerance;
            Core.EnlargeRatio = 100;
            this.DiagnoseObjs_List = diagnoseObjs_List;
            this.Color = color;
        }
        #endregion

        #region MTHD
        public bool DiagnoseLoop(Rhino.DocObjects.ObjRef objRef)
        {
            Rhino.Geometry.Brep brep = objRef.Brep();
            Rhino.DocObjects.RhinoObject brepObj = objRef.Object();
            List<int> combinedFacesCriminalIndex_List = new List<int>();

            if (brep != null)
            {
                Summary.Face_Count += brep.Faces.Count;
                Summary.Brep_Count ++;

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

                // Revise the Color ////////////////////////////////////////////////////////////////
                foreach (Diagnose diagnoseObj in DiagnoseObjs_List)
                {
                    combinedFacesCriminalIndex_List.AddRange(diagnoseObj.FacesCriminalIndex_List);
                    // Clean the list for next diagnose /////////////////
                    diagnoseObj.FacesCriminalIndex_List = new List<int>();
                }                
                combinedFacesCriminalIndex_List = combinedFacesCriminalIndex_List.Distinct().ToList();
                if (combinedFacesCriminalIndex_List.Count != 0)
                {
                    Summary.BrepIssue_Count++;
                    Summary.FaceIssue_Count += combinedFacesCriminalIndex_List.Count;
                    Core.ObjColorRevise(this.Color, brep, combinedFacesCriminalIndex_List, out Rhino.Geometry.Brep newBrep);
                    this.CurrentDoc.Objects.Replace(objRef, newBrep);

                }

                // Revise the Name /////////////////////////////////////////////////////////////////
                foreach (Diagnose diagnoseObj in DiagnoseObjs_List)
                {
                    if (diagnoseObj.BrepCriminalCheckResult)
                    {
                        Core.ObjNameRevise(brepObj, diagnoseObj.AccusationObjName);
                        diagnoseObj.BrepCriminalCount += 1;
                        diagnoseObj.BrepCriminalCheckResult = false;
                    }
                }
                brepObj.CommitChanges();

                //Finish one ObjRef/Brep Loop
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
        #endregion
    }

    static class Summary
    {
        #region ATTR

        public const string breakLine = "------------------------------------------------------ \n";
        public const string dialogTitle = "Inspection Result";
        public static int Face_Count = 0;
        public static int Brep_Count = 0;
        public static int FaceIssue_Count = 0;
        public static int BrepIssue_Count = 0;

        public static string DialogMessage { get; set; }
        public static string FaceCount_String { get; set; }
        public static string BrepCount_String { get; set; }
        public static string BrepIssue_String { get; set; }
        public static string FaceIssue_String { get; set; }
        public static string InspectionResult { get; set; }
        #endregion

        #region MTHD
        public static bool Result()
        {
            FaceCount_String = "The Total Faces Selected Count: " + Face_Count.ToString() + "\n";
            BrepCount_String = "The Total Breps Selected Count: " + Brep_Count.ToString() + "\n";
            BrepIssue_String = "Breps Have Deviant Components Count: " + BrepIssue_Count.ToString() + "\n";
            FaceIssue_String = "Faces Have Deviant Components Count: " + FaceIssue_Count.ToString() + "\n";


            DialogMessage =    breakLine +
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
        #endregion


    }

    public static class Accusation
    {
        public static string Curl = "Curl";
        public static string Extrusion = "Extrusion";
        public static string Vertical = "Vertical";
        public static string Redundency = "Redundency";
    }



}
