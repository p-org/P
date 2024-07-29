type tNominate = (voteFor: machine);

event eNominate : tNominate;

pure le(x: machine, y: machine): bool;
axiom forall (x: machine) :: le(x, x);
axiom forall (x: machine, y: machine) :: le(x, y) || le(y, x);
axiom forall (x: machine, y: machine, z: machine) :: (le(x, y) && le(y, z)) ==> le(x, z);
axiom forall (x: machine, y: machine) :: (le(x, y) && le(y, x)) ==> (x == y);

pure btw(x: machine, y: machine, z: machine): bool;
axiom forall (w: machine, x: machine, y: machine, z: machine) :: (btw(w, x, y) && btw(w, y, z)) ==> btw(w, x, z);
axiom forall (x: machine, y: machine, z: machine) :: btw(x, y, z) ==> !btw(x, z, y);
axiom forall (x: machine, y: machine, z: machine) :: btw(x, y, z) || btw(x, z, y) || (x == y) || (x == z) || (y == z);
axiom forall (x: machine, y: machine, z: machine) :: btw(x, y, z) ==> btw(y, z, x);

pure right(x: machine): machine;
axiom forall (x: machine) :: x != right(x);
axiom forall (x: machine, y: machine) :: (x != y && y != right(x)) ==> btw(x, right(x), y);

machine Server {
  start state Proposing {
    entry {
      send right(this), eNominate, (voteFor=this,);
    }
    on eNominate do (n: tNominate) {
      if (n.voteFor == this) {
        goto Won;
      } else if (le(this, n.voteFor)) {
        send right(this), eNominate, (voteFor=n.voteFor,);
      } else {
        send right(this), eNominate, (voteFor=this,);
      }
    }
  }
  state Won {
    ignore eNominate;
  }
}

// voteFor is the running max
invariant NoBypass: forall (n: machine, m: machine, e: eNominate) :: (inflight e && e targets m && n is Server && btw(e.voteFor, n, m)) ==> le(n, e.voteFor);
invariant SelfPendingMax: forall (n: machine, m: machine, e: eNominate) :: (inflight e && e targets m && e.voteFor == m) ==> le(n, m);

// Main theorems
invariant LeaderMax: forall (x: machine, y: machine) :: x is Won ==> le(y, x);
invariant UniqueLeader: forall (x: machine, y: machine) :: (x is Won && y is Won) ==> (x == y);