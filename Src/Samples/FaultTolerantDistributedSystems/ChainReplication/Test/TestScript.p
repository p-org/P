module ChainReplicationBasedSMR 
{ ChainReplicationNodeMachine, ChainReplicationMasterMachine , ChainReplicationFaultDetectionMachine, Timer }

module SMRClient { TestDriver1 }

module SMRReplicated { SMRReplicatedMachine }


//test 0: check that the chain replication protocol is safe
test Test0: (rename TestDriver1 to Main in (compose ChainReplicationBasedSMR, SMRClient, SMRReplicated));

