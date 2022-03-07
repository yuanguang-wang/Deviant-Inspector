import Rhino
import scriptcontext as sc
import System
import rhinoscriptsyntax as rs

def test_command():
    srf = rs.coercebrep(rs.GetObject())
    crv = srf.Faces[0].OuterLoop.To3dCurve().DuplicateSegments()
    pt = []
    for i in range(len(crv)):
        pt.append(crv[i].PointAtEnd)
    line = Rhino.Geometry.Line(pt[0],pt[1])
    ptPj = []
    distance = []
    for i in range(len(pt)):
        ptPj.append(line.ClosestPoint(pt[i],False))
        d = ptPj[i].DistanceToSquared(pt[i])
        distance.append(d)
    print(distance)
    #sc.doc.Objects.AddCurve(crv)
    sc.doc.Views.Redraw()


if __name__ == "__main__":
    test_command() # Call the function defined above