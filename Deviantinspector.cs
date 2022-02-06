using Rhino;
//using Rhino.Commands;
//using Rhino.Geometry;
//using Rhino.Input;
//using Rhino.Input.Custom;
using System;
using System.Collections.Generic;

namespace Deviant_Inspector
{
    public class Deviantinspector : Rhino.Commands.Command
    {
        public Deviantinspector()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static Deviantinspector Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "Deviantinspector";

        protected override Rhino.Commands.Result RunCommand(RhinoDoc doc, Rhino.Commands.RunMode mode)
        {
            string keyword = "rep";
            bool rc = Deviantinspector.ObjCollector(keyword, out Rhino.DocObjects.ObjRef[] objCollector);
            if (rc == false)
            {
                RhinoApp.WriteLine("ObjCollector Running Failure");
                return Rhino.Commands.Result.Failure;
            }
            string collectorLength = objCollector.Length.ToString();
            RhinoApp.WriteLine("Collector's Length is " + collectorLength);
            return Rhino.Commands.Result.Success;
        }

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
                    objectType = (Rhino.DocObjects.ObjectType) value[i];
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
    }
}
