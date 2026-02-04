// Define types
type tLogEntryAlternatingInt = (timestamp: int, key: int, value: int);

// Define events
event eNewAlternatingInt : tLogEntryAlternatingInt;

spec AlternatingInts observes eNewAlternatingInt {
  var expectedValues: map[int, int];

  start state InitialState {
    on eNewAlternatingInt do CheckAlternatingIntegers;
  }

  fun CheckAlternatingIntegers(logEntry: tLogEntryAlternatingInt) {
    var newKey : int;
    var newValue : int;
    var expectedVal: int;

    newKey = logEntry.key;
    newValue = logEntry.value;

    if(!(newKey in keys(expectedValues))) {
        expectedValues[newKey] = newValue;
    }

    expectedVal = expectedValues[newKey];

    if(expectedValues[newKey] == 0) {
      expectedValues[newKey] = 1;
    } else {
      expectedValues[newKey] = 0;
    }
    
    if(newValue != expectedVal) {
      assert false,
        format("Validation failed for key {0}! newValue: {1}, expectedValue: {2}", newKey, newValue, expectedVal);
    }
  }
}
