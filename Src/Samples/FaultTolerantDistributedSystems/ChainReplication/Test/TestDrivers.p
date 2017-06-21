machine SMRClientMachine : SMRClientInterface
sends eSMROperation;
{
	start state Init {
	}
}

machine SMRReplicatedMachine : SMRReplicatedMachineInterface 
sends eSMRResponse;
{
	start state Init {
	}
}