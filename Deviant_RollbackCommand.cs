using System;
using Rhino;

namespace Deviant_Inspector
{
    public class Deviant_RollbackCommand : Rhino.Commands.Command
    {
        public Deviant_RollbackCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static Deviant_RollbackCommand Instance { get; private set; }

        public override string EnglishName => "Deviantrollback";

        protected override Rhino.Commands.Result RunCommand(RhinoDoc doc, Rhino.Commands.RunMode mode)
        {
            // TODO: complete command.
            return Rhino.Commands.Result.Success;
        }
    }
}