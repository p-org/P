/*
This file implements the LBSharedObj machine that provides the lock (mutex) based shared object functionality.
--> Example: This is used for objects that are accessed using synchronized primitive in Java.
*/

event eAcquireLock: machine;
event eReleaseLock: (client:machine, val: any);
event eLockGranted: any;
event eRead: machine;
event eReadResp: any;

machine LBSharedObject {
  // machine that currently holds the lock over the obj
  var currHolder: machine;
  // contents of the shared obj
  var sharedObj: any;

  start state Init {
    entry(obj : any)
    {
      sharedObj = obj;
      goto WaitForAcquire;
    }
  }

  state WaitForAcquire {
    on eAcquireLock goto WaitForRelease with (client: machine)
    {
      var local_obj : any;
      send client, eLockGranted, sharedObj;
      currHolder = client;
    }

    on eRead do (client: machine) {
        send client, eReadResp, sharedObj;
    }
  }

  state WaitForRelease {
    defer eAcquireLock;
    on eReleaseLock goto WaitForAcquire with (payload: (client:machine, val: any)) {
      assert(payload.client == currHolder), "Release called by a machine that is not acquiring the lock";
        sharedObj = payload.val;
    }
    on eRead do (client: machine) {
      send client, eReadResp, sharedObj;
    }
  }
}

/*
AcquireLock function sends the eAcquireLock event to the LBSharedObj and waits/blocks until the lock is granted.
It returns a copy of the contents of the shared object.
*/
fun AcquireLock(sharedObj: LBSharedObject, client: machine) : any {
  var ret: any;
  send sharedObj, eAcquireLock, client;
  receive {
    case eLockGranted: (obj: any) {
      ret = obj;
    }
  }
  print format ("{0} got the lock on {1}", client, sharedObj);
  return ret;
}

/*
ReleaseLock function sends the eReleaseLock event along with the updated value of the contents of the shared obj.
*/
fun ReleaseLock(sharedObj: LBSharedObject, client: machine, val: any)
{
  send sharedObj, eReleaseLock, (client = client, val = val);
  print format ("Lock on {0} released by {1}", sharedObj, client);
}

/*
ReleaseLock function sends the eReleaseLock event along with the updated value of the contents of the shared obj.
*/
fun Read(sharedObj: LBSharedObject, client: machine): any
{
  var retVal: any;
  send sharedObj, eRead, client;
  receive {
    case eReadResp: (val: any) {
      retVal = val;
    }
  }
  print format ("{0} Read value of {1}: {2}", client, sharedObj, retVal);
  return retVal;
}