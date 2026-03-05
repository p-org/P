event doneInc;

machine User {
  start state Init {
    entry(payload: (machine, LBSharedObject))
    {
      var val: int;
      val = AcquireLock(payload.1, this) as int;
      ReleaseLock(payload.1, this, val+1);
      send payload.0, doneInc;
      raise halt;
    }
  }

}

machine TestDriver {
  var count: int;
  var sharedObj: LBSharedObject;
  start state Init {
    entry {
      var i : int;

      sharedObj = new LBSharedObject(1);
      // create 10 users
      while (i < 10)
      {
        new User(this, sharedObj);
        i = i + 1;
      }
    }
    on doneInc do {
      var val : int;
      count = count + 1;
      if(count == 10)
      {
        //assert that the value is 10
        val = AcquireLock(sharedObj, this) as int;
        assert val == 11, "Increment Didnt work!";
        ReleaseLock(sharedObj, this, val);
      }
    }
  }
}



test testLB_1 [main = TestDriver]: {User, TestDriver, LBSharedObject};
