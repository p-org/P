event eNext;
event PS : int;
event PR : int;

machine SyncCon1 {
  var N : int;
  var pc : map[int, int];
  var v : map[int, int];
  var Q : map[int, seq[int]];
  var up : map[int, bool];
  var pt : map[int, int];
  var t : map[int, bool];
  var d : map[int, int];
  var mb : map[int, seq[int]];
  var nodes : seq[int];
  var driver : machine;
  var FailNum : int;

  start state Init {
    entry (pld : (driver : machine, N : int, FAILNUM : int)) {
      var i : int;
      var sequence : seq[int];
      driver = pld.driver;
      N = pld.N;
      FailNum = pld.FAILNUM;
      while (i < N) {
        nodes += (i, i + 1);
        pc[nodes[i]] = 0;
        up[nodes[i]] = true;
        pt[nodes[i]] = 0;
        t[nodes[i]] = false;
        d[nodes[i]] = -1;
        mb[nodes[i]] = sequence;
        v[nodes[i]] = 0;
        Q[nodes[i]] = sequence;
        i = i + 1;
      }
    }
    on eNext do {
      var i : int;
      var j : int;
      var choices : seq[(int, int)];
      var forall : bool;
      while (i < N) {
        // P
        if(pc[nodes[i]] == 0 && (up[nodes[i]])) {
          v[nodes[i]] = nodes[i];
          Q[nodes[i]] = nodes;
          pc[nodes[i]] = 1;
        }
        // PS
        if (pc[nodes[i]] == 1) {
          // while cond
          if (up[nodes[i]] && (sizeof(Q[nodes[i]]) != 0)) {
            choices += (sizeof(choices), (nodes[i], 0));
          } else {
            // exit while cond
            if (up[nodes[i]]) {
              pt[nodes[i]] = pt[nodes[i]] + 1;
              pc[nodes[i]] = 2;
            }
          }
        }
        // PR
        else if (pc[nodes[i]] == 2) {
          // await
          if(up[nodes[i]]) {
            // forall
            forall = true; 
            j = 0;
            while (j < N) {
              if (up[nodes[j]]) {
                if (pt[nodes[j]] < pt[nodes[i]]) {
                  forall = false;
                }
              }
              j = j + 1;
            }
            if (forall) {
              choices += (sizeof(choices), (nodes[i], 1));
            }
          }
        }
        i = i + 1;
      }
      if (sizeof(choices) > 0) {
        i = choose(sizeof(choices));
        if(choices[i].1 == 0) {
          raise PS, choices[i].0;
        } else {
          raise PR, choices[i].0;
        }
      }
    } 

    on PS do (node : int) {
      var i : int;
      //while body
      print("while loop in PS");
      while(i < sizeof(Q[node])) {
        if (!(v[node] in mb[Q[node][i]])) {
          mb[Q[node][i]] += (sizeof(mb[Q[node][i]]), v[node]);
        }
        Q[node] -= i;
        // maybe fail
        if (FailNum > 0 && (up[node])) {
          if (choose()) {
            up[node] = false;
            FailNum = FailNum - 1;
          }
        }
        i = i + 1;
      }
      send driver, eNext;
    }

    on PR do (node : int) {
      var i : int;
      var min : int;
      print("PR");
      // get min mb
      while(i < sizeof(mb[node])) {
        if (mb[node][i] < min) {
          min = mb[node][i];
        }
        i = i + 1;
      }
      d[node] = min;
      t[node] = true;
      pc[node] = 3; // done
      send driver, eNext;
    }
  }
}

machine Main {
  var syncCon : machine;
  start state Init {
    entry {
     syncCon = new SyncCon1((driver=this, N=4, FAILNUM=2));
      send syncCon, eNext;
    }
    on eNext do {
      send syncCon, eNext;
    }
  }
}
