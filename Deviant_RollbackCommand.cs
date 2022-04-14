using Rhino;
using System.Collections.Generic;
using System.Linq;

namespace Deviant_Inspector
{
    public class Deviant_RollbackCommand : Rhino.Commands.Command
    {
        public Deviant_RollbackCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static Deviant_RollbackCommand Instance { get; private set; }

        public override string EnglishName => "Devro";

        protected override Rhino.Commands.Result RunCommand(RhinoDoc doc, Rhino.Commands.RunMode mode)
        {
            #region Initiation /////////////////////////////////////////////////////////////////////////////////////////
            Core.ModelTolerance = doc.ModelAbsoluteTolerance;

            Deviant_Inspector.Diagnose curl_Diagnose = new Diagnose(Accusation.Curl,
                                                                          Core.CurlCheck,
                                                                          true);
            Deviant_Inspector.Diagnose vertical_Diagnose = new Diagnose(Accusation.Vertical,
                                                                          Core.VerticalCheck,
                                                                          true);
            Deviant_Inspector.Diagnose extrusion_Diagnose = new Diagnose(Accusation.Extrusion,
                                                                          Core.ExtrusionCheck,
                                                                          true);
            Deviant_Inspector.Diagnose redundency_Diagnose = new Diagnose(Accusation.Redundency,
                                                                          Core.RedundencyCheck,
                                                                          false);
            List<Diagnose> diagnoseObjs_List = new List<Diagnose>
            {
                curl_Diagnose,
                vertical_Diagnose,
                extrusion_Diagnose,
                redundency_Diagnose
            };
            Deviant_Inspector.Inspection roller = new Inspection(doc, diagnoseObjs_List, CmdName.Rollback);
            Summary.Face_Count = 0;
            Summary.Brep_Count = 0;
            Summary.FaceIssue_Count = 0;
            Summary.BrepIssue_Count = 0;
            #endregion /////////////////////////////////////////////////////////////////////////////////////////////////

            #region Dispacth////////////////////////////////////////////////////////////////////////////////////////////
            bool cmdInterruption = roller.Selector(out Rhino.DocObjects.ObjRef[] objsRef_Arry);
            if (!cmdInterruption)
            {
                return Rhino.Commands.Result.Cancel;
            }
            Core.Dispatch(objsRef_Arry,
                          out List<Rhino.DocObjects.InstanceDefinition> iDef_List,
                          out List<Rhino.DocObjects.ObjRef> objRef_List);
            RhinoApp.WriteLine("Rollback Option is now Operating");
            System.Threading.Thread.Sleep(1000);
            #endregion /////////////////////////////////////////////////////////////////////////////////////////////////

            #region Brep ///////////////////////////////////////////////////////////////////////////////////////////////
            foreach (Rhino.DocObjects.ObjRef objRef in objRef_List)
            {
                roller.BrepDiagnoseLoop(objRef);
            }
            roller.InspectionResult();
            #endregion /////////////////////////////////////////////////////////////////////////////////////////////////

            #region Block //////////////////////////////////////////////////////////////////////////////////////////////            
            if (roller.BlockInspectionToggle)
            {
                foreach (Rhino.DocObjects.InstanceDefinition iDef in iDef_List)
                {
                    roller.IDefDiagnoseLoop(iDef);
                }
                roller.InspectionResult();
            }
            #endregion /////////////////////////////////////////////////////////////////////////////////////////////////
            
            RhinoApp.WriteLine("Rollback operation finished");
            doc.Views.Redraw();
            return Rhino.Commands.Result.Success;
        }
    }
}