/*
This file implements the ReentrantReadWriteLock based Shared Object (check the link below)
https://docs.oracle.com/javase/7/docs/api/java/util/concurrent/locks/ReentrantReadWriteLock.html

--> Example: This capability can be used to implement read-write lock based access to a shared lock in Java.

Note 1: The model does not support re-entrancy right now and leads to an assertion failure.
Note 2: The model does not support Lock downgrading i.e., a read request from a machine that holds a write lock will not be serviced and put on the waiting queue in the current semantics!

todo: Make sure that the implementation does not violate the above assumptions.
todo: Confirm it !!
*/

// events to interact with the Reentrant read-write lock based shared object.
event eAcqReadLock: machine;
event eReleaseReadLock: machine;
event eAcqWriteLock: machine;
event eReleaseWriteLock: (writer: machine, val: any);
event eRWLockGranted: any;

machine RWLBSharedObject {
  // waitlist of readers
  var waitingReaders : set[machine];
  // waitlist of writers
  var waitingWriters : set[machine];
  // current readers
  var currentReaders: set[machine];
  // current writer
  var currentWriter: machine;
    // contents of the shared obj
    var sharedObj: any;

  start state Init {
    entry (obj: any) {
      sharedObj = obj;
      goto ChooseReadOrWriteLock;
    }
  }

  state ChooseReadOrWriteLock {
    entry {
      var pickReaderOrWriter: set[bool];
      var pick: machine;
      if(sizeof(waitingWriters) > 0)
        pickReaderOrWriter += (true);
      if(sizeof(waitingReaders) > 0)
        pickReaderOrWriter += (false);

      if(sizeof(pickReaderOrWriter) > 0 && choose(pickReaderOrWriter))
      {
        pick = choose(waitingWriters);
        waitingWriters -= (pick);
        raise eAcqWriteLock, pick;
      }
      else if(sizeof(pickReaderOrWriter) > 0)
      {
        pick = choose(waitingReaders);
        waitingReaders -= (pick);
        raise eAcqReadLock, pick;
      }
      else
      {
        assert sizeof(waitingReaders) == 0 && sizeof(waitingWriters) == 0, "Logic for picking readers and writers is incorrect!";
      }
    }
    on eAcqReadLock goto ReadLockAcquired;
    on eAcqWriteLock goto WriteLockAcquired;
  }

  state ReadLockAcquired {

    entry (reader: machine) {
      // send grant request
      send reader, eRWLockGranted, sharedObj;
      // add to readers list
      currentReaders += (reader);
    }

    on eAcqReadLock do (reader: machine) {
      // send grant request
      send reader, eRWLockGranted, sharedObj;
      // reentrancy not supported right now
      assert !(reader in currentReaders), "Trying to re-acquire the lock (reentrancy not supported)";
      // add to readers list
      currentReaders += (reader);
    }

    on eReleaseReadLock do (reader: machine) {
      assert(reader in currentReaders), "Trying to release lock before acquiring it!!";
      currentReaders -= (reader);
      if(sizeof(currentReaders) == 0)
      {
        goto ChooseReadOrWriteLock;
      }
    }

    on eAcqWriteLock do (writer: machine)
    {
      waitingWriters += (writer);
    }
  }

  state WriteLockAcquired {
    entry (writer: machine){
      // send grant request
      send writer, eRWLockGranted, sharedObj;
      currentWriter = writer;
    }

    on eReleaseWriteLock do (payload : (writer: machine, val: any)){
      assert(payload.writer == currentWriter), "Trying to release lock before acquiring it!!";
      sharedObj = payload.val;
      currentWriter = null as machine;
      goto ChooseReadOrWriteLock;
    }

    on eAcqReadLock do (reader: machine)
    {
      waitingReaders += (reader);
    }

    on eAcqWriteLock do (writer: machine)
    {
      // re-entrancy not supported right now
      assert(writer != currentWriter), "Trying to re-acquire the lock (reentrancy not supported)";
      waitingWriters += (writer);
    }
  }
}





// Helper Functions to interact with the RWLBSharedObj


/*
AcquireReadLock function sends the eAcqReadLock event and waits for the lock to be granted.
It returns a copy of the shared object.
*/
fun AcquireReadLock(rwlock: RWLBSharedObject, client: machine) : any {
  var ret: any;
  send rwlock, eAcqReadLock, client;
  receive {
    case eRWLockGranted: (obj: any){
      print format ("Read Lock {0} Acquired by {1}", rwlock, client);
      ret = obj;
    }
  }
  return ret;
}

/*
ReleaseReadLock function sends the eReleaseReadLock event to the shared object.
*/
fun ReleaseReadLock(rwlock: RWLBSharedObject, client: machine) {
  send rwlock, eReleaseReadLock, client;
  print format ("Read Lock {0} Released by {1}", rwlock, client);
}

/*
AcquireWriteLock function sends the eAcqWriteLock event and waits for the lock to be granted.
It returns a copy of the shared object.
*/
fun AcquireWriteLock(rwlock: RWLBSharedObject, client: machine) : any {
  var retObj: any;
    send rwlock, eAcqWriteLock, client;
    receive {
      case eRWLockGranted: (obj: any) {
        print format ("Write Lock {0} Acquired by {1}", rwlock, client);
        retObj = obj;
      }
    }
    return retObj;
}

/*
ReleaseWriteLock function sends the eReleaseWriteLock event to the shared object.
It also updates the value of the shared object to the value passed by the client.
*/
fun ReleaseWriteLock(rwlock: RWLBSharedObject, client: machine, val: any) {
  send rwlock, eReleaseWriteLock, (writer = client, val = val);
  print format ("Write Lock {0} Released by {1}", rwlock, client);
}


