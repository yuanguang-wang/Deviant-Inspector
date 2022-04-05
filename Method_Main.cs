using System;
using System.Collections.Generic;
using System.Linq;



namespace Deviant_Inspector
{
    /// <summary>
    /// main method here
    /// </summary>
    public class Method_Main
    {
        // Delegates ///////////////////////////////////////////////////////////////////////////////////////////////////
        
        public delegate bool FaceDiagnoseDel(Rhino.Geometry.BrepFace bFace);

        // Attributes //////////////////////////////////////////////////////////////////////////////////////////////////
        public double ModelTolerance { get; set; }
        public int EnlargeRatio { get; set; }
        public bool Curl_Toggle { get; set; }
        public bool Vertical_Toggle { get; set; }
        public bool Extrusion_Toggle { get; set; }
        public bool Redundency_Toggle { get; set; }


        //Methods //////////////////////////////////////////////////////////////////////////////////////////////////////
        public bool ObjNameRevise(Rhino.DocObjects.RhinoObject rhObj, string accusation)
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

        public bool ObjNameRollback(Rhino.DocObjects.RhinoObject rhObj, string accusation)
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

        public bool ObjColorRevise(System.Drawing.Color color, 
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

        public bool ObjColorRollback(Rhino.Geometry.Brep brep, 
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

        public bool CurlCheck(Rhino.Geometry.BrepFace bFace) 
        {
            double relaviteTolerance = this.ModelTolerance * this.EnlargeRatio;
            if (bFace.IsPlanar(this.ModelTolerance) == false)
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

        public bool VerticalCheck(Rhino.Geometry.BrepFace bFace)
        {
            double relaviteTolerance = this.ModelTolerance * this.EnlargeRatio;
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
            if (this.ModelTolerance > distanceX)
            {
                if (this.ModelTolerance > distanceY)
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
                if (this.ModelTolerance > distanceY)
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

        public bool ExtrusionCheck(Rhino.Geometry.BrepFace bFace)
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
            double modelToleranceSquare = this.ModelTolerance * this.ModelTolerance;
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

        public bool RedundencyCheck(Rhino.Geometry.BrepFace bFace) 
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

        public bool Diagnose(Rhino.Geometry.Brep brep, 
                             out bool curlBrep_Result, 
                             out bool verticalBrep_Result, 
                             out bool redundencyBrep_Result, 
                             out bool extrusionBrep_Result, 
                             out int curlCriminalCount, 
                             out int verticalCriminalCount, 
                             out int extrusionCriminalCount, 
                             out int redundencyCriminalCount, 
                             out List<int> facesCriminalIndex_List)
        {
            curlBrep_Result = false;
            verticalBrep_Result = false;
            redundencyBrep_Result = false;
            extrusionBrep_Result = false;

            curlCriminalCount = 0;
            verticalCriminalCount = 0;
            extrusionCriminalCount = 0;
            redundencyCriminalCount = 0;

            facesCriminalIndex_List = new List<int>();

            foreach (Rhino.Geometry.BrepFace brepFace in brep.Faces)
            {
                if (brepFace != null)
                {
                    FaceDiagnoseDel curlFaceDiagnoseMethod = new FaceDiagnoseDel(CurlCheck);
                    curlBrep_Result = this.FaceDiagnose(brepFace, 
                                                        curlFaceDiagnoseMethod, 
                                                        Curl_Toggle, 
                                                        ref curlCriminalCount, 
                                                        ref facesCriminalIndex_List);

                    FaceDiagnoseDel verticalFaceDiagnoseMethod = new FaceDiagnoseDel(VerticalCheck);
                    verticalBrep_Result = this.FaceDiagnose(brepFace,
                                                        verticalFaceDiagnoseMethod,
                                                        Vertical_Toggle,
                                                        ref verticalCriminalCount,
                                                        ref facesCriminalIndex_List);

                    FaceDiagnoseDel extrusionFaceDiagnoseMethod = new FaceDiagnoseDel(ExtrusionCheck);
                    extrusionBrep_Result = this.FaceDiagnose(brepFace,
                                                        extrusionFaceDiagnoseMethod,
                                                        Extrusion_Toggle,
                                                        ref extrusionCriminalCount,
                                                        ref facesCriminalIndex_List);

                    FaceDiagnoseDel redundencyFaceDiagnoseMethod = new FaceDiagnoseDel(RedundencyCheck);
                    redundencyBrep_Result = this.FaceDiagnose(brepFace,
                                                        redundencyFaceDiagnoseMethod,
                                                        Redundency_Toggle,
                                                        ref redundencyCriminalCount,
                                                        ref facesCriminalIndex_List);

                    facesCriminalIndex_List = facesCriminalIndex_List.Distinct().ToList();

                }

            }

            return true;
        }

        public bool FaceDiagnose(Rhino.Geometry.BrepFace bFace, 
                                 FaceDiagnoseDel faceCheckMethod, 
                                 bool OptionToggle, 
                                 ref int faceCriminalCount,
                                 ref List<int> facesCriminalIndex_List)
        {
            bool brepCheckResult = false;
            if (OptionToggle)
            {
                if (faceCheckMethod(bFace)) // Return the faceCheckResult //
                {
                    brepCheckResult = true;
                    faceCriminalCount += 1;
                    facesCriminalIndex_List.Add(bFace.FaceIndex);
                }
            }
            return brepCheckResult;
        }

        public bool DiagnoseLoop(Rhino.Geometry.Brep brep)
        {

            return true;
        }


    }


    public class Summary
    {
        public int faceCriminalCount = 0;
        public int brepCriminalCount = 0;
        public string accusation;
        public string accusationObjName;

        public Summary(string accusation)
        {
            this.accusation = accusation;
            this.accusationObjName = "[" + accusation + "]";
        }

        public string InspectionResult(bool OptionToggle)
        {
            string faceCriminal_String;
            string brepCriminal_String;
            string breakLine = "------------------------------------------------------ \n";
            string summary_String;
            if (OptionToggle)
            {
                faceCriminal_String = "Faces with '" + accusation + "' Issue Count: " + faceCriminalCount.ToString() + "\n";
                brepCriminal_String = "Breps with '" + accusation + "' Issue Count: " + brepCriminalCount.ToString() + "\n";
                summary_String = breakLine +
                                 faceCriminal_String +
                                 brepCriminal_String;

            }
            else
            {
                summary_String = "";
            }
            return summary_String;
        }
    }

    public static class Accusation
    {
        public static string Curl = "Curl";
        public static string Extrusion = "Extrusion";
        public static string Vertical = "Vertical";
        public static string Redundency = "Redundency";
    }



}
