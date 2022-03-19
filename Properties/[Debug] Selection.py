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
    #go.GroupSelect = True
    #go.SubObjectSelect = False
    #go.EnableClearObjectsOnEntry(False)
    #go.EnableUnselectObjectsOnExit(False)
    #go.DeselectAllBeforePostSelect = False
    #go.EnablePreSelect(False, True)
    while True:
        res = go.Get()
        if res == Rhino.Input.GetResult.Option:
            print("option")
            continue
        elif res == Rhino.Input.GetResult.Object:
            print("obj")
            #go.EnablePreSelect(False, True)
            continue
        else:
            print("cancel")
            break
    
    


if __name__ == "__main__":
    test_command() # Call the function defined above