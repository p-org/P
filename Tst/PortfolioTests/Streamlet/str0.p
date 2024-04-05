event eNext;
event UpdateNChain : (node : int, next : int, e : int);
event Propose : (node : int, e : int);
event Vote : (node : int, e : int);

machine Str0 {
  var exists : map[int, bool];
  var nChain : map[int, seq[int]];
  var Msgs : seq[(chain : seq[int], epoch : int, sender : int)];
  var N : int;
  var EMAX : int;
  var driver : machine;

  start state Init {
    entry (pld : (driver : machine, N : int, EMAX : int)) {
      var i : int;
      var chain : seq[int];
      N = pld.N;
      EMAX = pld.EMAX;
      driver = pld.driver;
      while (i < N) {
        nChain[i] = chain;
        i = i + 1;
      }
      i = 0;
      while (i <= EMAX) {
        exists[i] = false;
        i = i + 1;
      }
      exists[0] = true;
      chain += (0, 0);
      Msgs += (0, (chain=chain, epoch=0, sender=0));
      Msgs += (1, (chain=chain, epoch=0, sender=1));
      Msgs += (2, (chain=chain, epoch=0, sender=2));
    }

    on eNext do {
      var i : int;
      var e : int;
      var choices : seq[(int, int, int)];
      while (i < N) {
        e = 0;
        while (e <= EMAX) {
          if ((mod(e, N) == i) && !exists[e]) {
            choices += (sizeof(choices), (i, 0, e));
          }
          else if (exists[e]) {
            choices += (sizeof(choices), (i, 1, e));
          }
          e = e + 1;
        }
        i = i + 1;
      }
      if (sizeof(choices) > 0) {
        i = choose(sizeof(choices));
        raise UpdateNChain, ((node=choices[i].0, next=choices[i].1, e=choices[i].2));
      }
    }
    on UpdateNChain do (pld : (node : int,  next : int, e : int)) {
      var i : int;
      var j : int;
      var emptySeq : seq[int];
      var chains : seq[int];
      var chain : seq[int];
      var maxLen : int;
      var count : int;
      var cond : bool;
      print("update N Chain");
      // first, find max chain length indices
      while (i < sizeof(Msgs)) {
        if(sizeof(Msgs[i].chain) > maxLen) {
          chains = emptySeq;
          maxLen = sizeof(Msgs[i].chain);
        }
        if (sizeof(Msgs[i].chain) == maxLen) {
          chains += (sizeof(chains), i);
        }
        i = i + 1;
      }
      chain = Msgs[chains[choose(sizeof(chains))]].chain;
      i = 0;
      // conditional
      while ((i <= EMAX) && !cond) {
        j = 0;
        count = 0;
        while (j < sizeof(Msgs)) {
          if ((Msgs[j].epoch == i) && (Msgs[j].chain == chain)) {
            count = count + 1;
          }
          j = j + 1;
        }
        if (2 * count > N) {
          cond = true;
        }
        i = i + 1;
      }
      if (cond) {
        nChain[pld.node] = chain;
      } else {
        i = 1;
        while (i < sizeof(chain)) {
          nChain[pld.node] += ((i - 1), chain[i]);
          i = i + 1;
        }
      }
      if (pld.next == 0) {
        raise Propose, (node=pld.node, e=pld.e);
      } else {
        raise Vote, (node=pld.node, e=pld.e);
      }
    }
    on Propose do (pld : (node : int, e : int)) {
      var i : int;
      var chain : seq[int];
      print("propose");
      chain += (0, pld.e);
      i = 1;
      while (i - 1 < sizeof(nChain[pld.node])) {
        chain += (i, nChain[pld.node][i - 1]);
        i = i + 1;
      }
      Msgs += (sizeof(Msgs), (chain=chain, epoch=pld.e, sender=pld.node));
      exists[pld.e] = true;
      send driver, eNext;
    }
    on Vote do (pld : (node : int, e : int)) {
      var i : int;
      var chain : seq[int];
      var choices : seq[int];
      print("vote");
      while (i < sizeof(Msgs)) {
        if (Msgs[i].epoch == pld.e) {
          choices += (sizeof(choices), i);
        }
        i = i + 1;
      }
      i = choose(sizeof(choices));
      chain = Msgs[choices[i]].chain;
      if (sizeof(chain) == sizeof(nChain[pld.node]) + 1) {
        if (!((chain=chain, epoch=pld.e, sender=pld.node) in Msgs)) {
          Msgs += (sizeof(Msgs), (chain=chain, epoch=pld.e, sender=pld.node));
        }
        exists[pld.e] = true;
        send driver, eNext;
      }
    }
  }
  fun mod (a : int, b : int) : int {
    return (a - b * (a / b));
  }
}

machine Main {
  var str0 : machine;
  start state Init {
    entry {
      str0 = new Str0((driver=this, N=3, EMAX=5));
      send str0, eNext;
    }
    on eNext do {
      send str0, eNext;
    }
  }
}
