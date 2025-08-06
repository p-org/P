// Define custom types
type tConfigEntryDualNodesBinary = (timestamp: int, key: int, node1Count: int, node2Count: int, totalValues: int);
type tLogEntryDualNodesBinary = (timestamp: int, key: int, node: int, value: int);
type tEntriesFinishedDualNodesBinary = (timestamp: int, key: int);

// Define events
event eConfigReceivedDualNodesBinary : tConfigEntryDualNodesBinary;
event eNewLogEntryDualNodesBinary : tLogEntryDualNodesBinary;
event eEntriesFinishedDualNodesBinary : tEntriesFinishedDualNodesBinary;


spec DualNodesBinary observes eConfigReceivedDualNodesBinary, eNewLogEntryDualNodesBinary, eEntriesFinishedDualNodesBinary {
  var node1Counters : map[int, int];
  var node2Counters : map[int, int];
  var totalCounters : map[int, int];
  
  var expectedNode1Counts : map[int, int];
  var expectedNode2Counts : map[int, int];
  var expectedTotalValues : map[int, int];
  var key: int;

  start state InitialState {
    on eConfigReceivedDualNodesBinary do SaveConfiguration;
    on eNewLogEntryDualNodesBinary do ValidateLogEntry;
    on eEntriesFinishedDualNodesBinary do ValidateFinalCount;
  }

  fun SaveConfiguration(config: tConfigEntryDualNodesBinary) {
    // Expected values
    expectedNode1Counts[config.key] = config.node1Count;
    expectedNode2Counts[config.key] = config.node2Count;
    expectedTotalValues[config.key] = config.totalValues;
    
    // Initialize actual counters
    node1Counters[config.key] = 0;
    node2Counters[config.key] = 0;
    totalCounters[config.key] = 0;
  }

  fun ValidateLogEntry(logEntry: tLogEntryDualNodesBinary) {
    key = logEntry.key;
    if(!(key in keys(totalCounters))) {
      assert false, format("Key provided for ValidateLogEntry has not been initialized. Key received: {0}", key);
    }

    totalCounters[key] = totalCounters[key] + 1;

    if(logEntry.node == 1 && logEntry.value == 1) {
        node1Counters[key] = node1Counters[key] + 1;
    }

    if(logEntry.node == 2 && logEntry.value == 1) {
        node2Counters[key] = node2Counters[key] + 1;
    }
  }
  
  fun ValidateFinalCount(finalEntry: tEntriesFinishedDualNodesBinary) {
    var key: int;
    var errorMessage: string;
    var errorFound: bool;
    key = finalEntry.key;
    errorFound = false;
  
    if (!(key in totalCounters)) {
      assert false, format("Key provided for ValidateFinalCount does not exist. Key: {0}", key);
    }
    
    // Check for incorrect counts and build error message
    if (node1Counters[key] != expectedNode1Counts[key]) {
      errorMessage = errorMessage + format("node1 expected value: {0}, actual value: {1}; ", expectedNode1Counts[key], node1Counters[key]);
      errorFound = true;
    }
    if (node2Counters[key] != expectedNode2Counts[key]) {
      errorMessage = errorMessage + format("node2 expected value: {0}, actual value: {1}; ", expectedNode2Counts[key], node2Counters[key]);
      errorFound = true;
    }
    if (totalCounters[key] != expectedTotalValues[key]) {
      errorMessage = errorMessage + format("total expected value: {0}, actual value: {1}; ", expectedTotalValues[key], totalCounters[key]);
      errorFound = true;
    }
    
    // Assert false if any error found
    if (errorFound) {
      assert false, format("Incorrect counts for key {0}: {1}", key, errorMessage);
    }
  }
}
