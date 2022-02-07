﻿using Rhino;
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
            bool rc = ArchivedMethods.ObjCollector(keyword, out Rhino.DocObjects.ObjRef[] objCollector);
            if (rc == false)
            {
                RhinoApp.WriteLine("ObjCollector Running Failure");
                return Rhino.Commands.Result.Failure;
            }
            string collectorLength = objCollector.Length.ToString();
            RhinoApp.WriteLine("Collector's Length is " + collectorLength);
            return Rhino.Commands.Result.Success;
        }

    }
}
