// Define types
type tLogEntryIncreasingInt = (timestamp: int, key: int, value: int);

// Define events
event eNewIncreasingInt : tLogEntryIncreasingInt;

spec IncreasingInts observes eNewIncreasingInt {

  var highestValues : map[int, int];  // Store the highest value read from logs for each key

  start state InitialState {
    on eNewIncreasingInt do UpdateIntegers;
  }

  fun UpdateIntegers(logEntry: tLogEntryIncreasingInt) {
    var newKey : int;
    var newValue : int;

    newKey = logEntry.key;
    newValue = logEntry.value;

    if (!(newKey in keys(highestValues))) {  // If the key is not found in our map
      highestValues[newKey] = newValue;
    } else {
      if (newValue <= highestValues[newKey]) {
        assert false, format("Validation failed for key {0}! newValue: {1}, highestValue: {2}", newKey, newValue, highestValues[newKey]);
      } else {
        highestValues[newKey] = newValue;
      }
    }
  }
}
