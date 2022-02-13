using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;

namespace Deviant_Inspector
{
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

        public static bool FlatSrfCheck(Rhino.Geometry.BrepFace bFace, double modelTolerance, int enlargeRatio, out bool flatSrfTrigger) 
        {
            flatSrfTrigger = false;
            double relaviteTolerance = modelTolerance * enlargeRatio;
            if (bFace.IsPlanar(modelTolerance) == false)
            {
                // RhinoApp.WriteLine("model tolerance is false");
                if (bFace.IsPlanar(relaviteTolerance) == true)
                {
                    flatSrfTrigger = true;
                    // RhinoApp.WriteLine("relavite tolerance is true");
                }
            }
            return true;
        }
    }
}
