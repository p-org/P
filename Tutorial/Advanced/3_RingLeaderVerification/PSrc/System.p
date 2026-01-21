type tNominate = (voteFor: machine);

event eNominate : tNominate;

pure le(x: machine, y: machine): bool;
init-condition forall (x: machine) :: le(x, x);
init-condition forall (x: machine, y: machine) :: le(x, y) || le(y, x);
init-condition forall (x: machine, y: machine, z: machine) :: (le(x, y) && le(y, z)) ==> le(x, z);
init-condition forall (x: machine, y: machine) :: (le(x, y) && le(y, x)) ==> (x == y);

Lemma less_than {
  invariant le_refl: forall (x: machine) :: le(x, x);
  invariant le_symm: forall (x: machine, y: machine) :: le(x, y) || le(y, x);
  invariant le_trans: forall (x: machine, y: machine, z: machine) :: (le(x, y) && le(y, z)) ==> le(x, z);
  invariant le_antisymm: forall (x: machine, y: machine) :: (le(x, y) && le(y, x)) ==> (x == y);
}
Proof {
  prove less_than;
}

pure btw(x: machine, y: machine, z: machine): bool;
init-condition forall (w: machine, x: machine, y: machine, z: machine) :: (btw(w, x, y) && btw(w, y, z)) ==> btw(w, x, z);
init-condition forall (x: machine, y: machine, z: machine) :: btw(x, y, z) ==> !btw(x, z, y);
init-condition forall (x: machine, y: machine, z: machine) :: btw(x, y, z) || btw(x, z, y) || (x == y) || (x == z) || (y == z);
init-condition forall (x: machine, y: machine, z: machine) :: btw(x, y, z) ==> btw(y, z, x);

Lemma between_rel {
  invariant btw_1: forall (w: machine, x: machine, y: machine, z: machine) :: (btw(w, x, y) && btw(w, y, z)) ==> btw(w, x, z);
  invariant btw_2: forall (x: machine, y: machine, z: machine) :: btw(x, y, z) ==> !btw(x, z, y);
  invariant btw_3: forall (x: machine, y: machine, z: machine) :: btw(x, y, z) || btw(x, z, y) || (x == y) || (x == z) || (y == z);
  invariant btw_4: forall (x: machine, y: machine, z: machine) :: btw(x, y, z) ==> btw(y, z, x);
}
Proof {
  prove between_rel;
}

pure right(x: machine): machine;
init-condition forall (x: machine) :: x != right(x);
init-condition forall (x: machine, y: machine) :: (x != y && y != right(x)) ==> btw(x, right(x), y);
init-condition forall (x: machine, y: machine) :: !btw(x, y, right(x));
init-condition forall (x: machine, n: machine, m: machine) :: m == right(n) ==> (btw(n, m, x) || x == m || x == n);

Lemma right_rel {
  invariant right_neq_self: forall (x: machine) :: x != right(x);
  invariant btw_right: forall (x: machine, y: machine) :: (x != y && y != right(x)) ==> btw(x, right(x), y);
  invariant Aux1: forall (x: machine, y: machine) :: !btw(x, y, right(x));
  invariant right_btw: forall (x: machine, n: machine, m: machine) :: m == right(n) ==> (btw(n, m, x) || x == m || x == n);
}
Proof {
  prove right_rel;
}

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
Lemma lemmas {
  invariant LeaderMax: forall (x: machine, y: machine) :: x is Won ==> le(y, x);
  invariant Aux: forall (x: machine, y: machine) :: (le(x, y) && le(y, x)) ==> x == y;
  invariant NoBypass: forall (n: machine, m: machine, e: eNominate) :: (inflight e && e targets m && btw(e.voteFor, n, m)) ==> le(n, e.voteFor);
  invariant SelfPendingMax: forall (n: machine, m: machine, e: eNominate) :: (inflight e && e targets m && e.voteFor == m) ==> le(n, m);
}
Proof {
  prove lemmas using less_than, between_rel, right_rel;
}

// Main theorems
Theorem Safety {
  invariant UniqueLeader: forall (x: machine, y: machine) :: (x is Won && y is Won) ==> x == y;
}
Proof {
  prove Safety using lemmas_LeaderMax, lemmas_Aux;
  prove default;
}