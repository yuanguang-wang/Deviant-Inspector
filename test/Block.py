import Rhino
import scriptcontext as sc
import System
import rhinoscriptsyntax as rs

def test_command():
    go = Rhino.Input.Custom.GetObject()
    go.SetCommandPrompt("test")
    go.GetMultiple(1, 0)
    objref = go.Objects()
    geo = []
    tran = []
    for i in range(len(objref)):
        geo.append(objref[i].Geometry())
        #print(type(geo[i]))
        tran.append(Rhino.Geometry.Brep.TryConvertBrep(geo[i]))
        print(type(tran[i]))
    """
    for i in range(len(objref)):
        brep = tran[i].DuplicateBrep()
        brep.Faces[1].PerFaceColor = System.Drawing.Color.Red
        sc.doc.Objects.Replace(objref[i], brep)
    """
    #sc.doc.Objects.Add(tran[i])
    sc.doc.Views.Redraw()


if __name__ == "__main__":
    test_command() # Call the function defined above