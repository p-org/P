// Simple failure injector machine that can crash participant nodes
machine FailureInjector {
  // Set of participant nodes that could be crashed
  var nodes: set[Participant];
  // Number of failures to inject
  var numFailures: int;
  
  start state Init {
    entry Init_Entry;
  }
  
  state InjectFailures {
    entry InjectFailures_Entry;
  }
  
  fun Init_Entry(config: (nodes: set[Participant], nFailures: int)) {
    nodes = config.nodes;
    numFailures = config.nFailures;
    goto InjectFailures;
  }
  
  fun InjectFailures_Entry() {
    var participants_seq: seq[Participant];
    var random_index: int;
    var selected_participant: Participant;
    var i: int;
    
    // If no failures to inject, just exit
    if(numFailures <= 0) {
      return;
    }
    
    // Convert the set to a sequence for random selection
    participants_seq = default(seq[Participant]);
    foreach(selected_participant in nodes) {
      participants_seq += (sizeof(participants_seq), selected_participant);
    }
    
    // Inject the specified number of failures
    i = 0;
    while(i < numFailures && sizeof(participants_seq) > 0) {
      // Select a random participant to fail
      random_index = choose(sizeof(participants_seq));
      selected_participant = participants_seq[random_index];
      
      // Remove the selected participant from our list
      participants_seq -= (random_index);
      
      print format("Injecting failure for participant {0}", selected_participant);
      
      // In a real implementation, this would actually crash the participant
      // However, since we're modeling a simplified system without fault tolerance,
      // we'll just note that a failure was injected
      
      i = i + 1;
    }
  }
}

// Module for failure injection
module FailureInjector = { FailureInjector };