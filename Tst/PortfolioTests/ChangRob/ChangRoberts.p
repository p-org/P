event eNext;

machine ChangRoberts {
  var nodes : seq[int];
  var ids : map[int, int];
  var msgs : map[int, seq[int]];
  var newMsgs : seq[int];
  var driver : machine;
  var initiator : map[int, bool];
  var st : map[int, int];
  var pc : map[int, int];
  start state Init {
    entry (pld : (driver : machine, N : int, Id : map[int, int])) {
      var i : int;
      var emptySeq : seq[int];
      driver = pld.driver;
      ids = pld.Id;
      while (i < pld.N) {
        nodes += (i, i + 1);
        msgs += (i + 1, emptySeq);
        initiator += (i + 1, choose());
        if (initiator[i + 1]) {
          st += (i + 1, 0);
        } else {
          st += (i + 1, 1);
        }
        pc += (i + 1, 0);
        i = i + 1;
      }
    }
    on eNext do {
      var i : int;
      var succ : int;
      var j : int;
      var choices : seq[(int, int, int, int)];
      var newMsgs : seq[int];
      while (i < sizeof(nodes)) {
        if (i + 1 == sizeof(nodes)) {
          succ = 0;
        } else {
          succ = i + 1;
        }
        if (pc[nodes[i]] == 0) {
          if(initiator[nodes[i]]) {
            if (!(ids[nodes[i]] in msgs[nodes[succ]])) {
              choices += (sizeof(choices), (0, nodes[i], nodes[succ], ids[nodes[i]])); 
            }
          } else {
            pc[nodes[i]] = 1;
          }
        } else {
          while (j < sizeof(msgs[nodes[i]])) {
            if (!(msgs[nodes[i]][j] in msgs[nodes[succ]]) &&
                ((st[nodes[i]] == 1) || msgs[nodes[i]][j] < ids[nodes[i]])) {
              choices += (sizeof(choices), (1, nodes[i], nodes[succ], msgs[nodes[i]][j]));
            } else {
              if (st[nodes[i]] != 2) {
                if(msgs[nodes[i]][j] == ids[nodes[i]]) {
                  choices += (sizeof(choices), (2, nodes[i], 0, msgs[nodes[i]][j]));
                }
              }
            }
            j = j + 1;
          }
        }
        i = i + 1;
      }
      if (sizeof(choices) > 0) {
        i = choose(sizeof(choices));
        if (choices[i].0 == 0) {
          print("n0");
          msgs[choices[i].2] += (sizeof(msgs[choices[i].2]), choices[i].3);
          pc[choices[i].1] = 1;
        } else {
          // first msg update
          j = 0;
          while (j < sizeof(msgs[choices[i].1])) {
            if (msgs[choices[i].1][j] != choices[i].3) {
              newMsgs += (sizeof(newMsgs), msgs[choices[i].1][j]);
            }
            j = j + 1;
          }
          msgs[choices[i].1] = newMsgs;
          if (choices[i].0 == 1) {
            print("n1");
            st[choices[i].1] = 1;
            msgs[choices[i].2] += (sizeof(msgs[choices[i].2]), choices[i].3);
          } else {
            print("win");
            st[choices[i].1] = 2;
          } 
        }
        send driver, eNext;
      } 
    }
  }
}
machine Main {
  var changRoberts : machine;
  start state Init {
    entry {
      var ids : map[int,int];
      ids += (1, 3);
      ids += (2, 2);
      ids += (3, 7);
      ids += (4, 8);
      ids += (5, 9);
      ids += (6, 10);
      ids += (7, 1);
      ids += (8, 4);
      ids += (9, 6);
      ids += (10, 5);
      changRoberts = new ChangRoberts((driver=this, N=6, Id=ids));
      send changRoberts, eNext;
    }
    on eNext do {
      send changRoberts, eNext;
    }
  }
}
