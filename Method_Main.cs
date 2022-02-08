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

        public static bool FlatSrfCheck(Rhino.Geometry.Surface srf, double modelTolerance, int enlargeRatio, out bool flatSrfTrigger) 
        {
            flatSrfTrigger = false;
            double relaviteTolerance = modelTolerance * enlargeRatio;
            if (srf.IsPlanar(modelTolerance) == false)
            {
                // RhinoApp.WriteLine("model tolerance is false");
                if (srf.IsPlanar(relaviteTolerance) == true)
                {
                    flatSrfTrigger = true;
                    // RhinoApp.WriteLine("relavite tolerance is true");
                }
            }
            return true;
        }
    }
}
