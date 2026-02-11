//event doneInc;

machine User2 {
  start state Init {
    entry(payload: (machine, RWLBSharedObject))
    {
      var val: int;
      val = AcquireWriteLock(payload.1, this) as int;
      ReleaseWriteLock(payload.1, this, val+1);
      send payload.0, doneInc;
    }
  }

}

machine TestDriver2 {
  var count: int;
  var sharedObj: RWLBSharedObject;
  start state Init {
    entry {
      var i : int;

      sharedObj = new RWLBSharedObject(1);
      // create 10 users
      while (i < 10)
      {
        new User2(this, sharedObj);
        i = i + 1;
      }
    }
    on doneInc do {
      var val : int;
      count = count + 1;
      if(count == 10)
      {
        //assert that the value is 10
        val = AcquireReadLock(sharedObj, this) as int;
        assert val == 11, "Increment Didnt work!";
        ReleaseReadLock(sharedObj, this);
      }
    }
  }
}

test testRWLB_1 [main = TestDriver2]: {User2, TestDriver2, RWLBSharedObject};