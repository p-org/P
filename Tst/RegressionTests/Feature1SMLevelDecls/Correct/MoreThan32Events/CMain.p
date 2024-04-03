//1st goup of 30 events (plus dafault events null and halt)
event ArtifactManagerGoalStateAcheived;
event ArtifactManagerGoalStateNotAcheived;
event CreateAssemblyManager;
event CreateAssemblyManagerComplete;
event ArtifactManagerInit;
event TerminateArtifactManager;
event UpdateArtifactManager;
event ArtifactManagerGoalAchieved;
event ArtifactComplete;
event ArtifactStarted;
event ArtifactRequiredNow;
event ReconcileArtifacts;
event BeginArtifact;
event ArtifactAssemblyFailed;
event BeginRemoveStaleArtifactParts;
event BeginArtifactPart;
event CancelArtifact;
event BeginArtifactMerge;
event RemoveStaleArtifactPartsComplete;
event DownloadStarted;
event DownloadComplete;
event DownloadCompleteInternal;
event ArtifactMergeComplete;
event BeginDownload;
event BeginDownloadInternal;
event CancelDownload;
event BeginClonePart;
event InitializeOperationManager;
event OperationStatusUpdate;
event OperationListUpdate;

//2nd group of 32 events
event TerminateOperationManager;
event QueryArtifactsOperation;
event DeleteArtifactOperation;
event UpdateArtifactOperation;
event QueryArtifactPartsOperation;
event DeleteArtifactPartOperation;
event AssembleArtifactOperation;
event ClonePartFromArtifactOperation;
event DownloadArtifactPartOperation;
event AttemptCancelDownloadOperation;
event QueryArtifactsOperationComplete;
event DeleteArtifactOperationComplete;
event UpdateArtifactOperationComplete;
event QueryArtifactPartsOperationComplete;
event DeleteArtifactPartOperationComplete;
event AssembleArtifactOperationComplete;
event ClonePartFromArtifactOperationComplete;
event DownloadArtifactPartOperationComplete;
event AttemptCancelDownloadOperationComplete;
event E1;
event E2;
event E3;
event E4;
event E5;
event E6;
event E7;
event E8;
event E9;
event E10;
event E11;
event E12;
event E13;

machine DownloadManagerMachine {
    start state Begin
    {
    on BeginDownload do {}
	on E6 do {}
    }

    state QueryingArtifacts
    {
    on CancelDownload do {}
    on QueryArtifactsOperationComplete do {}
    on BeginClonePart do {}
    on BeginDownloadInternal do {}
    on DownloadCompleteInternal do {}
    }

    state CloneArtifactPart
    {
	on halt do {}
	on null do {}
    on CancelDownload do {}
    on ClonePartFromArtifactOperationComplete do {}
    on BeginDownloadInternal do {}
    on DownloadCompleteInternal do {}
	on E1 do {}
	on E5 do {}
	on E13 do {}
    }

    state Downloading
    {
    on CancelDownload do {}
    on DownloadArtifactPartOperationComplete do {}
    on DownloadCompleteInternal do {}
    }

    state Canceling
    {
    // TODO: Missing CancelDownload?
    on DownloadCompleteInternal do {}
    on QueryArtifactsOperationComplete do {}
    on ClonePartFromArtifactOperationComplete do {}
    on DownloadArtifactPartOperationComplete do {}
    on AttemptCancelDownloadOperationComplete do {}
	on E2 do {}
	on E8 do {}
	on E11 do {}
    }

    // Stop State:
    state Complete
    {
        entry
        {
            raise halt;
        }
    }
}



machine Main {
	    // Fields:



	    // States:

	    start state Init
	    {
	        entry
	        {
	
	        }
	    }

	}

	







