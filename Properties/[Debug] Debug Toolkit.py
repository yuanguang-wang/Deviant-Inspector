import Rhino
import scriptcontext as sc
import System
import rhinoscriptsyntax as rs

def test_command():
    srf = rs.GetObject()
    srf = rs.coercebrep(srf)
    print(type(srf))
    loop = srf.Faces[0].OuterLoop.To3dCurve()
    sc.doc.Objects.AddCurve(loop)
    sc.doc.Views.Redraw()
    #Rhino.UI.Dialogs.ShowMessageBox("Inspection Result", "Nearly Flat Surface Count: 250")



if __name__ == "__main__":
    test_command() # Call the function defined above