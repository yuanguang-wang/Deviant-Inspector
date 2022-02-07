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
    }
}
