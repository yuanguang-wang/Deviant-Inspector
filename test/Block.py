import Rhino
import scriptcontext as sc
import System
import rhinoscriptsyntax as rs

def test_command():
    
    go = Rhino.Input.Custom.GetObject()
    go.SetCommandPrompt("test")
    go.GeometryFilter = Rhino.DocObjects.ObjectType.InstanceReference
    go.GetMultiple(1, 0)
    objref = go.Objects()
    geo = []
    tran = []
    print(go.ObjectCount)
    idefs= []
    for i in range(len(objref)):
        geo.append(objref[i].Object())
        print(type(geo[i]))
        idef = geo[i].InstanceDefinition
        if idef not in idefs:
            idefs.append(idef)
            
    geo = []
    for i in range(len(idefs)):
        geo.append(idefs[i].GetObjects())
        print(idefs[i].Index)
        breps = []
        #attrs = []
        for j in range(len(geo[i])):
            brep = geo[i][j].Geometry.DuplicateBrep()
            brep.Faces[1].PerFaceColor = System.Drawing.Color.Red
            #attrs.append(brep.Attributes)
            breps.append(brep)
        #Rhino.DocObjects.Tables.InstanceDefinitionTable.ModifyGeometry(idefs[i].Index, breps)
        sc.doc.InstanceDefinitions.ModifyGeometry(idefs[i].Index, breps)

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