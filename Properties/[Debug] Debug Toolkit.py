import Rhino
import scriptcontext as sc
import System

def test_command():
    Rhino.UI.Dialogs.ShowMessageBox("Inspection Result", "Nearly Flat Surface Count: 250")



if __name__ == "__main__":
    test_command() # Call the function defined above