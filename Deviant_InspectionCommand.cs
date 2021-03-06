using Rhino;
using System.Collections.Generic;
using System.Linq;

namespace Deviant_Inspector
{
    public class Deviant_InspectionCommand : Rhino.Commands.Command
    {
        public Deviant_InspectionCommand()
        {        
            Instance = this;
        }

        public static Deviant_InspectionCommand Instance { get; private set; }

        public override string EnglishName => "Devin";

        protected override Rhino.Commands.Result RunCommand(RhinoDoc doc, Rhino.Commands.RunMode mode)
        {

            #region ColorSet ///////////////////////////////////////////////////////////////////////////////////////////
            RhinoApp.WriteLine("Select One Color to be Drawn on Deviants, Default Color is Red");
            System.Drawing.Color passedColor = System.Drawing.Color.Red;
            bool result_Color = Rhino.UI.Dialogs.ShowColorDialog(ref passedColor,
                                                                 true,
                                                                "Select One Color to be Drawn on Deviants, " +
                                                                "Default is Red");
            if (result_Color == false)
            {
                RhinoApp.WriteLine("[COMMAND EXIT] Deviant Color is not Specified");
                doc.Views.Redraw();
                return Rhino.Commands.Result.Cancel;
            }
            #endregion /////////////////////////////////////////////////////////////////////////////////////////////////

            #region Initiation /////////////////////////////////////////////////////////////////////////////////////////
            Core.ModelTolerance = doc.ModelAbsoluteTolerance;
            Core.Color = passedColor;
            
            Deviant_Inspector.Diagnose curl_Diagnose = new Diagnose(Accusation.Curl,
                                                                          Core.CurlCheck,
                                                                          true);
            Deviant_Inspector.Diagnose vertical_Diagnose = new Diagnose(Accusation.Vertical,
                                                                          Core.VerticalCheck,
                                                                          true);
            Deviant_Inspector.Diagnose extrusion_Diagnose = new Diagnose(Accusation.Extrusion,
                                                                          Core.ExtrusionCheck,
                                                                          true);
            Deviant_Inspector.Diagnose Redundancy_Diagnose = new Diagnose(Accusation.Redundancy,
                                                                          Core.RedundancyCheck,
                                                                          false);
            List<Diagnose> diagnoseObjs_List = new List<Diagnose>
            {
                curl_Diagnose,
                vertical_Diagnose,
                extrusion_Diagnose,
                Redundancy_Diagnose
            };
            Deviant_Inspector.Inspection inspector = new Inspection(doc, diagnoseObjs_List, CmdName.Inspection);
            Summary.Face_Count = 0;
            Summary.Brep_Count = 0;
            Summary.FaceIssue_Count = 0;
            Summary.BrepIssue_Count = 0;
            #endregion /////////////////////////////////////////////////////////////////////////////////////////////////

            #region Dispacth////////////////////////////////////////////////////////////////////////////////////////////
            bool cmdInterruption = inspector.Selector(out Rhino.DocObjects.ObjRef[] objsRef_Arry);
            if (!cmdInterruption)
            {
                return Rhino.Commands.Result.Cancel;
            }
            Core.Dispatch(objsRef_Arry,
                          out List<Rhino.DocObjects.InstanceDefinition> iDef_List,
                          out List<Rhino.DocObjects.ObjRef> objRef_List);
            #endregion /////////////////////////////////////////////////////////////////////////////////////////////////

            #region Brep ///////////////////////////////////////////////////////////////////////////////////////////////
            foreach (Rhino.DocObjects.ObjRef objRef in objRef_List)
            {
                inspector.BrepDiagnoseLoop(objRef);
            }
            inspector.InspectionResult();
            #endregion /////////////////////////////////////////////////////////////////////////////////////////////////

            #region Block //////////////////////////////////////////////////////////////////////////////////////////////            
            if (inspector.BlockInspectionToggle)
            {
                foreach (Rhino.DocObjects.InstanceDefinition iDef in iDef_List)
                {
                    inspector.IDefDiagnoseLoop(iDef);
                }
                inspector.InspectionResult();
            }
            else
            {
                RhinoApp.WriteLine("Blocks are not Selected");
            }
            #endregion /////////////////////////////////////////////////////////////////////////////////////////////////

            #region Summary ////////////////////////////////////////////////////////////////////////////////////////////
            doc.Views.Redraw();
            Summary.Result();
            #endregion /////////////////////////////////////////////////////////////////////////////////////////////////

            return Rhino.Commands.Result.Success;
        }
    }
}
