using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST
{
    public interface IPModuleExpr : IPAST
    {
        //Attributes of module expression

        //// Module Signature 
        ModulePrivateEvents::= (mod: ModuleExpr, ev: NonNullEventName).
	ModulePrivateInterfaces::= (mod: ModuleExpr, mach: String).
	ModuleSends::= (mod: ModuleExpr, ev: NonNullEventName).
	ModuleReceives::= (mod: ModuleExpr, ev: NonNullEventName).
	ModuleCreates::= (mod: ModuleExpr, i: String).

	//// Module Code Gen and Compatibity Helpers
	ModuleLinkMap::= (mod: ModuleExpr, newMachineName: String, createdInterface: String, newImpMachine: String).
	ModuleMachineDefMap::= (mod: ModuleExpr, newName: String, impMachine: String).  // for both machines and monitors
	ModuleSafeMap::= (mod: ModuleExpr, newName: String, isSafe: Boolean).
	ModuleMonitorMap::= (mod: ModuleExpr, newMonitorName: String, impMachine: String).

    I

    }
}

