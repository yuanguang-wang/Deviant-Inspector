import Rhino
import scriptcontext as sc
import System
import rhinoscriptsyntax as rs

def test_command():
    srf = rs.coercebrep(rs.GetObject())
    crv = srf.Faces[0].OuterLoop.To3dCurve().DuplicateSegments()[0]
    sc.doc.Objects.AddCurve(crv)
    sc.doc.Views.Redraw()


if __name__ == "__main__":
    test_command() # Call the function defined above