using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;

namespace Deviant_Inspector
{
    public class Method_Archived
    {
        public static bool ObjCollector(string keyword, out Rhino.DocObjects.ObjRef[] objCollector)
        {
            //Initiation an obj
            Rhino.Input.Custom.GetObject getObjects = new Rhino.Input.Custom.GetObject
            {
                DeselectAllBeforePostSelect = false,
                GroupSelect = true,
                SubObjectSelect = false
            };
            int[] value = (int[])Enum.GetValues(typeof(Rhino.DocObjects.ObjectType));
            int i = 0;
            Rhino.DocObjects.ObjectType objectType;
            //Pick Specified Enumeration Member by input string
            foreach (string name in Enum.GetNames(typeof(Rhino.DocObjects.ObjectType)))
            {
                if (name.Contains(keyword))
                {
                    //This should work only once
                    objectType = (Rhino.DocObjects.ObjectType)value[i];
                    i--;
                    getObjects.GeometryFilter = objectType;
                    break;
                }
                i++;
            }
            //Exception: Keyword Detector Failure
            if (i == value.Length)
            {
                RhinoApp.WriteLine("Specified ObjectType Keyword Not Found in Rhino.DocObjects.ObjectType Enumeration");
                objCollector = null;
                return false;
            }
            //Selection Action
            getObjects.SetCommandPrompt("Select Objects being Inspected, Press Enter after Selection");
            Rhino.Input.GetResult selectionResult = getObjects.GetMultiple(1, 0);
            //Exception: Selection Failure
            if (selectionResult != Rhino.Input.GetResult.Object)
            {
                RhinoApp.WriteLine("Selection Process has been Interrupted");
                objCollector = null;
                return false;
            }
            objCollector = getObjects.Objects();
            return true;
        }

        public static bool NameColorResetTool(Rhino.DocObjects.RhinoObject obj)
        {
            obj.Attributes.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
            obj.Attributes.ObjectColor = System.Drawing.Color.Red;
            obj.Attributes.Name = "test name";
            obj.CommitChanges();
            
            return true;
        }

        public static bool ObjAttrRevise(Rhino.DocObjects.RhinoObject rhObj, string newName)
        {
            //Color Revision
            rhObj.Attributes.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
            rhObj.Attributes.ObjectColor = System.Drawing.Color.Red;
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

        public static bool SrfCollector(Rhino.Geometry.Brep brep, out List<Rhino.Geometry.Surface> srf_List)
        {
            // Trigger Setting
            /*
            bool flatSrfTrigger = false;
            bool abVertiTrigger = false;
            bool redunCPTrigger = false;
            bool extuCrvTrigger = false;
            */
            srf_List = new List<Rhino.Geometry.Surface>();
            Rhino.Geometry.Collections.BrepFaceList brepFace_List = brep.Faces;
            foreach (Rhino.Geometry.BrepFace brepFace in brepFace_List)
            {
                srf_List.Add(brepFace.UnderlyingSurface());
            }
            return true;
        }

        public static bool FaceCollector(Rhino.Geometry.Brep brep, out List<Rhino.Geometry.BrepFace> face_List)
        {
            // Trigger Setting
            /*
            bool flatSrfTrigger = false;
            bool abVertiTrigger = false;
            bool redunCPTrigger = false;
            bool extuCrvTrigger = false;
            */
            face_List = new List<Rhino.Geometry.BrepFace>();
            Rhino.Geometry.Collections.BrepFaceList brepFace_List = brep.Faces;
            foreach (Rhino.Geometry.BrepFace brepFace in brepFace_List)
            {
                face_List.Add(brepFace);
            }
            return true;
        }

        public static bool ExtrudeCheck(Rhino.Geometry.Curve[] crvSegs, double modelTolerance)
        {

            double modelToleranceSquare = modelTolerance * modelTolerance;
            List<Rhino.Geometry.Point3d> pt_List = new List<Rhino.Geometry.Point3d>();
            Rhino.Geometry.Curve segLast = crvSegs.Last();
            Rhino.Geometry.Point3d ptBase = segLast.PointAtEnd;
            pt_List.Add(ptBase);
            // Query Max Distance
            foreach (Rhino.Geometry.Curve segment in crvSegs)
            {
                Rhino.Geometry.Point3d endPt = segment.PointAtEnd;
                foreach (Rhino.Geometry.Point3d pt in pt_List)
                {
                    double distance = endPt.DistanceToSquared(pt);
                    if (distance > modelToleranceSquare)
                    {
                        pt_List.Add(endPt);
                    }
                }
            }
            Rhino.Geometry.Point3d ptLast = pt_List.Last();
            double distanceMax = ptLast.DistanceToSquared(ptBase);
            Rhino.Geometry.Point3d ptLongestStart = ptBase;
            Rhino.Geometry.Point3d ptLongestEnd = ptLast;
            foreach (Rhino.Geometry.Point3d pt_i in pt_List)
            {
                foreach (Rhino.Geometry.Point3d pt_j in pt_List)
                {
                    double distance = pt_i.DistanceToSquared(pt_j);
                    if (distance > distanceMax)
                    {
                        distanceMax = distance;
                        ptLongestStart = pt_i;
                        ptLongestEnd = pt_j;
                    }
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
            bool preCheck = Method_Archived.ExtrudeCheck(crvSegsSliced, modelTolerance);
            if (preCheck)
            {
                bool fullCheck = Method_Archived.ExtrudeCheck(crvSegs, modelTolerance);
                if (fullCheck)
                {
                    return true;
                }
            }
            return false;
        }

        //public bool Diagnose(Rhino.Geometry.Brep brep,
        //                     out bool curlBrep_Result,
        //                     out bool verticalBrep_Result,
        //                     out bool redundencyBrep_Result,
        //                     out bool extrusionBrep_Result,
        //                     out int curlCriminalCount,
        //                     out int verticalCriminalCount,
        //                     out int extrusionCriminalCount,
        //                     out int redundencyCriminalCount,
        //                     out List<int> facesCriminalIndex_List)
        //{
        //    curlBrep_Result = false;
        //    verticalBrep_Result = false;
        //    redundencyBrep_Result = false;
        //    extrusionBrep_Result = false;

        //    curlCriminalCount = 0;
        //    verticalCriminalCount = 0;
        //    extrusionCriminalCount = 0;
        //    redundencyCriminalCount = 0;

        //    facesCriminalIndex_List = new List<int>();

        //    foreach (Rhino.Geometry.BrepFace brepFace in brep.Faces)
        //    {
        //        if (brepFace != null)
        //        {
        //            // Flat Surface Iteration //////////////////////////
        //            if (Curl_Toggle)
        //            {
        //                bool curlFace_Result = this.CurlCheck(brepFace);
        //                if (curlFace_Result)
        //                {
        //                    curlBrep_Result = true;
        //                    curlCriminalCount++;
        //                    facesCriminalIndex_List.Add(brepFace.FaceIndex);
        //                }
        //            }
        //            // Vertical Surface Iteration //////////////////////
        //            if (Vertical_Toggle)
        //            {
        //                bool verticalFace_Result = this.VerticalCheck(brepFace);
        //                if (verticalFace_Result)
        //                {
        //                    verticalBrep_Result = true;
        //                    verticalCriminalCount++;
        //                    facesCriminalIndex_List.Add(brepFace.FaceIndex);
        //                }
        //            }
        //            // Extruded Surface Iteration //////////////////////
        //            if (Extrusion_Toggle)
        //            {
        //                bool extrusionFace_Result = this.ExtrusionCheck(brepFace);
        //                if (extrusionFace_Result)
        //                {
        //                    extrusionBrep_Result = true;
        //                    extrusionCriminalCount++;
        //                    facesCriminalIndex_List.Add(brepFace.FaceIndex);
        //                }
        //            }
        //            // Redundency Surface Iteration //////////////////////
        //            if (Redundency_Toggle)
        //            {
        //                bool redundencyFace_Result = this.RedundencyCheck(brepFace);
        //                if (redundencyFace_Result)
        //                {
        //                    redundencyBrep_Result = true;
        //                    redundencyCriminalCount++;
        //                    facesCriminalIndex_List.Add(brepFace.FaceIndex);
        //                }
        //            }

        //            facesCriminalIndex_List = facesCriminalIndex_List.Distinct().ToList();

        //        }

        //    }

        //    return true;

        //}

    }
}
