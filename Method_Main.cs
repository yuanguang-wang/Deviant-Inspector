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
        public bool ObjNameRevise(Rhino.DocObjects.RhinoObject rhObj, string newName)
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

        public bool ObjColorRevise(System.Drawing.Color color, Rhino.Geometry.Brep brep, List<int> criminalIndex_List, out Rhino.Geometry.Brep newBrep)
        {
            newBrep = brep.DuplicateBrep();
            foreach (int i in criminalIndex_List)
            {
                newBrep.Faces[i].PerFaceColor = color;
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
            Rhino.Geometry.Curve[] crvSegs = bFace.OuterLoop.To3dCurve().DuplicateSegments();
            if (crvSegs.Length <= 2)
            {
                return false;
            }
            
            // Point of the Outer Loop Collection //////////////////////////////////////////
            double modelToleranceSquare = this.ModelTolerance * this.ModelTolerance;
            List<Rhino.Geometry.Point3d> pt_List = new List<Rhino.Geometry.Point3d>();
            foreach (Rhino.Geometry.Curve segment in crvSegs)
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
            // Avoiding Bad Objects ////////////////////////////////////////////////////////
            Rhino.Geometry.Curve[] crvSegs = bFace.OuterLoop.To3dCurve().DuplicateSegments();
            if (crvSegs.Length <= 2)
            {
                return false;
            }

            // Itirate each Loop Segment ///////////////////////////////////////////////////
            foreach (Rhino.Geometry.Curve segment in crvSegs)
            {
                Rhino.Geometry.Curve crvDup = segment.DuplicateCurve();
                var crvSimplified = segment.Simplify(Rhino.Geometry.CurveSimplifyOptions.All, ModelTolerance, 1);
                if (crvSimplified != null)
                {
                    Rhino.RhinoApp.WriteLine("simplified");
                    bool crvDuplicationDetect = Rhino.Geometry.GeometryBase.GeometryEquals(crvSimplified, crvDup);
                    Rhino.RhinoApp.WriteLine(crvDuplicationDetect.ToString());
                    if (crvDuplicationDetect == false)
                    {
                        Rhino.RhinoApp.WriteLine("run");
                        return true;
                    }
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
            this.accusationObjName = " " + accusation + " |";
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


}
