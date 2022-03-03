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
    }
}
