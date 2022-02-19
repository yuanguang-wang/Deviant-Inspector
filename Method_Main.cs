using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;

namespace Deviant_Inspector
{
    /// <summary>
    /// Summary Info:
    /// Brep Count: xxxxx
    /// Brep_issue1: xxxx &%
    /// Brep_issue2: xxxx &%
    /// Brep_issues: xxxx &%
    /// BrepFace Count: xxxxxx
    /// BrepFace_issue1: xxxxx &%
    /// BrepFace_issues: xxxxx &%
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

        public static bool VerticalCheck(Rhino.Geometry.BrepFace bFace, double modelTolerance, out bool triggerVertical)
        {
            triggerVertical = true;
            return true;
        }
    }
}
