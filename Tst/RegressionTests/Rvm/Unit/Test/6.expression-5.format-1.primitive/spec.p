event event1;
event event2;
event event3;
event event1Int: int;
event event2Int: int;
event event3Int: int;

event getState;

spec unittest observes event1, event2, event3, event1Int, event2Int, event3Int, getState {
  start state Init {
    on event1 do {
      var res: string;

      res = format("Value: {0}.", 7);
      assert (res == "Value: 7."), format("Expected \"Value: 7.\" but got \"{0}\".", res);

      res = format("Value: {0}.", 7.0);
      assert (res == "Value: 7.0."), format("Expected \"Value: 7.0.\" but got \"{0}\".", res);

      res = format("Value: {0}.", false);
      assert (res == "Value: false."), format("Expected \"Value: false.\" but got \"{0}\".", res);
    }
  }
}
