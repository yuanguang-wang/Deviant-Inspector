using System;
using System.Collections.Generic;
using System.Linq;
using Rhino;


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

        public static bool ObjNameRevise(Rhino.DocObjects.RhinoObject rhObj, string newName)
        {
            //Name Revision
            if (rhObj.Attributes.Name == null)
            {
                rhObj.Attributes.Name = "|";
            }
            string currentName = rhObj.Attributes.Name;
            if (!currentName.Contains(newName))
            {
                newName = currentName + newName;
            }
            rhObj.Attributes.Name = newName;
            rhObj.CommitChanges();

            return true;
        }

        public static bool ObjColorRevise(System.Drawing.Color color, Rhino.Geometry.Brep brep, List<int> criminalIndex_List, out Rhino.Geometry.Brep newBrep)
        {
            newBrep = brep.DuplicateBrep();
            foreach (int i in criminalIndex_List)
            {
                newBrep.Faces[i].PerFaceColor = color;
            }

            return true;
        }

        public static bool FlatSrfCheck(Rhino.Geometry.BrepFace bFace, double modelTolerance, int enlargeRatio) 
        {
            double relaviteTolerance = modelTolerance * enlargeRatio;
            if (bFace.IsPlanar(modelTolerance) == false)
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

        public static bool VerticalCheck(Rhino.Geometry.BrepFace bFace, double modelTolerance, int enlargeRatio)
        {
            double relaviteTolerance = modelTolerance * enlargeRatio;
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
            if (modelTolerance > distanceX)
            {
                if (modelTolerance > distanceY)
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
                if (modelTolerance > distanceY)
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

        public static bool ExtrudeCheck(Rhino.Geometry.Curve[] crvSegs, double modelTolerance)
        {
            List<double> distance_List = new List<double>();
            List<Rhino.Geometry.Point3d> pt_List = new List<Rhino.Geometry.Point3d>();
            // Query Max Distance
            foreach (Rhino.Geometry.Curve segment in crvSegs)
            {
                Rhino.Geometry.Point3d startPt = segment.PointAtStart;
                pt_List.Add(startPt);
                Rhino.Geometry.Point3d endPt = segment.PointAtEnd;
                pt_List.Add(endPt);
                distance_List.Add(startPt.DistanceToSquared(endPt));
            }           
            int segmentIndex = distance_List.IndexOf(distance_List.Max());
            Rhino.Geometry.Curve crvLongest = crvSegs[segmentIndex];
            foreach (Rhino.Geometry.Point3d pt in pt_List)
            {
                crvLongest.ClosestPoint(pt, out double t);
                Rhino.Geometry.Point3d ptOnCrv = crvLongest.PointAt(t);
                double distance = ptOnCrv.DistanceToSquared(pt);
                double tolerance = modelTolerance * modelTolerance;
                if (distance > tolerance)
                {
                    return false;                   
                }
            }
            return true;
        }

        public static bool ExtrudeDoubleCheck(Rhino.Geometry.BrepFace bFace, double modelTolerance)
        {
            Rhino.Geometry.Curve[] crvSegs = bFace.OuterLoop.To3dCurve().DuplicateSegments();
            if (crvSegs.Length < 2)
            {
                // Fail Safe;
                return false;
            }
            Rhino.Geometry.Curve[] crvSegsSliced = crvSegs.Take(2).ToArray();
            bool preCheck = Method_Main.ExtrudeCheck(crvSegsSliced, modelTolerance);
            if (preCheck)
            {
                bool fullCheck = Method_Main.ExtrudeCheck(crvSegs, modelTolerance);
                if (fullCheck)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
