import Rhino
import scriptcontext as sc
import System
import rhinoscriptsyntax as rs

def test_command():
    pt1 = rs.coerce3dpoint(rs.GetPoint("pt1"))
    pt2 = rs.coerce3dpoint(rs.GetPoint("pt2"))
    pt3 = rs.coerce3dpoint(rs.GetPoint("pt3"))
    line = Rhino.Geometry.Line(pt1, pt2)
    pt4 = line.ClosestPoint(pt3, False)
    sc.doc.Objects.AddPoint(pt4)
    sc.doc.Views.Redraw()
    Rhino.Geometry.Brep.Vertices



if __name__ == "__main__":
    test_command() # Call the function defined above