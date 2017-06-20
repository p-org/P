module ChainReplicationBasedSMR 
{ ChainReplicationNodeMachine, ChainReplicationMasterMachine , ChainReplicationFaultDetectionMachine, Timer }

module SMRClient { SMRClientMachine }

module SMRReplicated { SMRReplicatedMachine }


//test 0: check that the chain replication protocol is safe
test Test0: (rename SMRClientMachine to Main in (compose ChainReplicationBasedSMR, SMRClient, SMRReplicated));

