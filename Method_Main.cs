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
    
    public class Core
    {

        // ATTR ////////////////////////////////////////////////////////////////////////////////////////////////////////
        public double ModelTolerance { get; set; }
        public int EnlargeRatio { get; set; }
        
        // MTHD ////////////////////////////////////////////////////////////////////////////////////////////////////////
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


    }


    public class Diagnose
    {
        // Class used for multicasting not only one object, Current is 4 ///////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        public int FaceCriminalCount { get; set; }
        public int BrepCriminalCount { get; set; }
        public List<int> FacesCriminalIndex_List { get; set; }

        // ATTR ////////////////////////////////////////////////////////////////////////////////////////////////////////
        public string Accusation { get; set; }
        public string AccusationObjName { get; set; }
        public bool FaceCriminalCheckResult { get; set; }
        public bool BrepCriminalCheckResult { get; set; }
        public bool Option_Toggle { get; set; }
        public FaceDiagnoseDel CoreMethodHandler { get; set; }
        public Rhino.Geometry.BrepFace CurrentFace { get; set; }
        public Rhino.Geometry.Brep CurrentBrep { get; set; }
        
        
        // CTOR ////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Diagnose(string accusation, FaceDiagnoseDel faceDiagnoseMethod, bool optionToggle)
        {
            this.Accusation = accusation;
            this.AccusationObjName = "[" + accusation + "]";
            this.CoreMethodHandler = faceDiagnoseMethod;
            this.Option_Toggle = optionToggle;
            this.FaceCriminalCount = 0;
            this.BrepCriminalCount = 0;
            this.FacesCriminalIndex_List = new List<int>();
        }

        // MTHD ////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Face Diagnose, Runtime Type: Multi //////////////////////////////////////////////////////////////////////////

        public bool FaceDiagnose()
        {
            this.BrepCriminalCheckResult = false;
            if (Option_Toggle)
            {
                if (CoreMethodHandler(CurrentFace)) // Return the faceCheckResult //
                {
                    this.BrepCriminalCheckResult = true;
                    this.FaceCriminalCount += 1;
                    this.FacesCriminalIndex_List.Add(CurrentFace.FaceIndex);
                }
            }
            return this.BrepCriminalCheckResult;
        }
        


        public string InspectionResult(bool OptionToggle)
        {
            string faceCriminal_String;
            string brepCriminal_String;
            string breakLine = "------------------------------------------------------ \n";
            string summary_String;
            if (OptionToggle)
            {
                faceCriminal_String = "Faces with '" + Accusation + "' Issue Count: " + FaceCriminalCount.ToString() + "\n";
                brepCriminal_String = "Breps with '" + Accusation + "' Issue Count: " + BrepCriminalCount.ToString() + "\n";
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

    public class Inspection
    {
        public Rhino.RhinoDoc CurrentDoc { get; set; }

        public bool DiagnoseLoop(Rhino.Geometry.Brep brep)
        {
            // Diagnose Object using Accusation Name ////////////////////////////////////////////////////////////////////
            Core cm = new Core();

            Deviant_Inspector.Diagnose curl_Diagnose = new Diagnose(Accusation.Curl, cm.CurlCheck, true);
            Deviant_Inspector.Diagnose vertical_Diagnose = new Diagnose(Accusation.Vertical, cm.VerticalCheck, true);
            Deviant_Inspector.Diagnose extrusion_Diagnose = new Diagnose(Accusation.Extrusion, cm.ExtrusionCheck, true);
            Deviant_Inspector.Diagnose redundency_Diagnose = new Diagnose(Accusation.Redundency, cm.RedundencyCheck, true);

            List<int> facesCriminalIndex_List = new List<int>();
            int face_Count = 0;

            if (brep != null)
            {
                face_Count += brep.Faces.Count;

                foreach (Rhino.Geometry.BrepFace bFace in brep.Faces)
                {
                    if (bFace != null)
                    {
                        curl_Diagnose.FaceDiagnose();
                        vertical_Diagnose.FaceDiagnose();
                        extrusion_Diagnose.FaceDiagnose();
                        redundency_Diagnose.FaceDiagnose();
                    }

                }
                facesCriminalIndex_List.AddRange(curl_Diagnose.FacesCriminalIndex_List);

                facesCriminalIndex_List = facesCriminalIndex_List.Distinct().ToList();
            }
            return true;
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
