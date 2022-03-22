import Rhino
import scriptcontext as sc
import System

def test_brep_perface_color():
    # Select a face
    go = Rhino.Input.Custom.GetObject()
    go.SetCommandPrompt("Select surface")
    go.GeometryFilter = Rhino.DocObjects.ObjectType.Surface
    go.SubObjectSelect = True
    go.Get()
    if go.CommandResult() != Rhino.Commands.Result.Success:
        return
    
    objref = go.Object(0)
    # Get the brep face that was picked
    face = objref.Face()
    # Get the owning brep
    brep = objref.Brep()
    if face and brep:
        # Only make sense if there is more than 1 face
        if brep.Faces.Count > 1:
            new_brep = brep.DuplicateBrep()
            new_brep.Faces[face.FaceIndex].PerFaceColor = System.Drawing.Color.Red
            sc.doc.Objects.Replace(objref.ObjectId, new_brep)
            sc.doc.Views.Redraw()

if __name__ == "__main__":
    test_brep_perface_color() # Call the function defined above