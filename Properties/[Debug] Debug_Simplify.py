import Rhino
import scriptcontext as sc
import System
import rhinoscriptsyntax as rs

def test_command():
    #crv = rs.coercecurve(rs.GetObjects())
    #crv = crv.Simplify(Rhino.Geometry.CurveSimplifyOptions.All , 0.01, 1)
    #sc.doc.Objects.Add(crv)
    
    crv1 = rs.coercecurve(rs.GetObjects())
    crv2 = rs.coercecurve(rs.GetObjects())
    print(Rhino.Geometry.GeometryBase.GeometryEquals(crv1,crv2))
    #sc.doc.Objects.AddCurve(crv)
    sc.doc.Views.Redraw()


if __name__ == "__main__":
    test_command() # Call the function defined above