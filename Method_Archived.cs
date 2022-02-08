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
    }
}
