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
        /// <summary>
        /// This Method Changes the RhinoObject's Color and Name
        /// </summary>
        /// <param name="rhObj">Rhino.DocObjects.RhinoObject</param>
        /// <param name="newName">System.String</param>
        /// <returns>returns true if worked, use #Rhino.DocObjects.RhinoObject.CommitChange# </returns>
        
        // Public Attributes //////////////////////////////////////////////////////////////////
        public double ModelTolerance { get; set; }
        public int EnlargeRatio { get; set; }

        //Methods /////////////////////////////////////////////////////////////////////////////
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

        public bool ObjColorRevise(System.Drawing.Color color, Rhino.Geometry.Brep brep, List<int> criminalIndex_List, out Rhino.Geometry.Brep newBrep)
        {
            newBrep = brep.DuplicateBrep();
            foreach (int i in criminalIndex_List)
            {
                newBrep.Faces[i].PerFaceColor = color;
            }

            return true;
        }

        public bool ObjColorRollback(Rhino.Geometry.Brep brep, List<int> criminalIndex_List, out Rhino.Geometry.Brep newBrep)
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
            Rhino.Geometry.Curve[] loop = bFace.OuterLoop.To3dCurve().DuplicateSegments();
            if (loop.Length <= 2)
            {
                return false;
            }
            
            // Point of the Outer Loop Collection //////////////////////////////////////////
            double modelToleranceSquare = this.ModelTolerance * this.ModelTolerance;
            List<Rhino.Geometry.Point3d> pt_List = new List<Rhino.Geometry.Point3d>();
            foreach (Rhino.Geometry.Curve segment in loop)
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

    public class Scan
    {

        public List<Rhino.Geometry.Brep> Brep_List { get; set; }
        public bool CurlBrep_Result { get; set; }
        public bool VerticalBrep_Result { get; set; }
        public bool RedundencyBrep_Result { get; set; }
        public bool ExtrusionBrep_Result { get; set; }
        public Rhino.Input.Custom.OptionToggle Curl_Toggle { get; set; }
        public Rhino.Input.Custom.OptionToggle Vertical_Toggle { get; set; }
        public Rhino.Input.Custom.OptionToggle Extrusion_Toggle { get; set; }
        public Rhino.Input.Custom.OptionToggle Redundency_Toggle { get; set; }

        public System.Drawing.Color Color { get; set; } 
        public Rhino.RhinoDoc CurrentDoc { get; set; }
        public Rhino.DocObjects.ObjRef[] ObjsRef_Arry { get; set; }

        // Scan Methods ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////

        public bool Check()
        {
            // MM Instance Initiation ///////////////////////////////////////////////////////////////////////
            Deviant_Inspector.Method_Main mm = new Method_Main
            {
                ModelTolerance = CurrentDoc.ModelAbsoluteTolerance,
                EnlargeRatio = 100
            };

            // Summary for using Accusation Name ////////////////////////////////////////////////////////////
            Deviant_Inspector.Summary extrusion_Summary = new Summary(Accusation.Extrusion);
            Deviant_Inspector.Summary curl_Summary = new Summary(Accusation.Curl);
            Deviant_Inspector.Summary vertical_Summary = new Summary(Accusation.Vertical);
            Deviant_Inspector.Summary redundency_Summary = new Summary(Accusation.Redundency);

            // Summary Initiation //////////////////////////////////////////////////////////////////////////
            int brepIssue_Count = 0;
            int faceIssue_Count = 0;
            //int brep_Count = Brep_List.Count;
            //int face_Count = 0;

            // Scan Operation ///////////////////////////////////////////////////////////////////////////////
            int i = 0;
            foreach (Rhino.Geometry.Brep brep in Brep_List)
            {
                List<int> facesCriminalIndex_List = new List<int>();

                CurlBrep_Result = false;
                VerticalBrep_Result = false;
                RedundencyBrep_Result = false;
                ExtrusionBrep_Result = false;

                foreach (Rhino.Geometry.BrepFace brepFace in brep.Faces)
                {
                    // Flat Surface Iteration //////////////////////////
                    if (Curl_Toggle.CurrentValue)
                    {
                        bool curlFace_Result = mm.CurlCheck(brepFace);
                        if (curlFace_Result)
                        {
                            CurlBrep_Result = true;
                            curl_Summary.faceCriminalCount++;
                            facesCriminalIndex_List.Add(brepFace.FaceIndex);
                        }
                    }
                    // Vertical Surface Iteration //////////////////////
                    if (Vertical_Toggle.CurrentValue)
                    {
                        bool verticalFace_Result = mm.VerticalCheck(brepFace);
                        if (verticalFace_Result)
                        {
                            VerticalBrep_Result = true;
                            vertical_Summary.faceCriminalCount++;
                            facesCriminalIndex_List.Add(brepFace.FaceIndex);
                        }
                    }
                    // Extruded Surface Iteration //////////////////////
                    if (Extrusion_Toggle.CurrentValue)
                    {
                        bool extrusionFace_Result = mm.ExtrusionCheck(brepFace);
                        if (extrusionFace_Result)
                        {
                            ExtrusionBrep_Result = true;
                            extrusion_Summary.faceCriminalCount++;
                            facesCriminalIndex_List.Add(brepFace.FaceIndex);
                        }
                    }
                    // Extruded Surface Iteration //////////////////////
                    if (Redundency_Toggle.CurrentValue)
                    {
                        bool redundencyFace_Result = mm.RedundencyCheck(brepFace);
                        if (redundencyFace_Result)
                        {
                            RedundencyBrep_Result = true;
                            redundency_Summary.faceCriminalCount++;
                            facesCriminalIndex_List.Add(brepFace.FaceIndex);
                        }
                    }

                    // Color Change & Commit ////////////////////////////////
                    if (CurlBrep_Result ||
                        VerticalBrep_Result ||
                        RedundencyBrep_Result ||
                        ExtrusionBrep_Result
                       )
                    {
                        facesCriminalIndex_List = facesCriminalIndex_List.Distinct().ToList();
                        brepIssue_Count++;
                        faceIssue_Count += facesCriminalIndex_List.Count;
                        mm.ObjColorRevise(Color, brep, facesCriminalIndex_List, out Rhino.Geometry.Brep newBrep);
                        CurrentDoc.Objects.Replace(ObjsRef_Arry[i], newBrep);

                    }
                }

                i++;
            }
            return true;
        }

    }

}
