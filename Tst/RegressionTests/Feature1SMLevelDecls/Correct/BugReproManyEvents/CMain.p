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




machine DownloadManagerMachine {

    start state Begin
    {
    on BeginDownload do {}
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
    on CancelDownload do {}
    on ClonePartFromArtifactOperationComplete do {}
    on BeginDownloadInternal do {}
    on DownloadCompleteInternal do {}
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

	







