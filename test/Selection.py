import Rhino
import scriptcontext as sc
import System
import rhinoscriptsyntax as rs

def test_command():

    #sc.doc.Objects.UnselectAll()
    #sc.doc.Views.Redraw()
    vertical_Toggle = Rhino.Input.Custom.OptionToggle(True, "Off", "On")
    go = Rhino.Input.Custom.GetObject()
    go.SetCommandPrompt("test")
    go.AddOptionToggle('Vertical', vertical_Toggle)
    #go.GroupSelect = True
    #go.SubObjectSelect = False
    #go.EnableClearObjectsOnEntry(False)
    ##############################################
    go.EnableUnselectObjectsOnExit(False) # Principle
    go.DeselectAllBeforePostSelect = False # Action
    ##############################################
    #go.EnablePreSelect(False, True)
    while True:
        res = go.GetMultiple(1, 0)
        if res == Rhino.Input.GetResult.Option:
            go.EnablePreSelect(False, True)
            print("option")
            continue
        elif res == Rhino.Input.GetResult.Object:
            print("obj")
            go.EnablePreSelect(True, True)
            break
        else:
            print("cancel")
            break
    


if __name__ == "__main__":
    test_command() # Call the function defined above