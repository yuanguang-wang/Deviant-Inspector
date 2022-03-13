import Rhino
import scriptcontext as sc
import System
import rhinoscriptsyntax as rs

def test_command():
    int = 300
    opt_int = Rhino.Input.Custom.OptionInteger(int, 200, 900)
    go = Rhino.Input.Custom.GetObject()
    go.SetCommandPrompt("test")
    go.AddOptionInteger("Option1", opt_int)
    go.GroupSelect = true
    go.SubObjectSelect = false
    go.EnableClearObjectsOnEntry(false)
    go.EnableUnselectObjectsOnExit(false)
    go.DeselectAllBeforePostSelect = false
    


if __name__ == "__main__":
    test_command() # Call the function defined above