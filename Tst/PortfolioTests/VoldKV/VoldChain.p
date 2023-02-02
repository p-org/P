event eNext;
event CLR : int;
event R : int;
event CLW : int;
event W : int;
event NM : int;
event NEM : int;
event NDF : int;
event AddNode : int;
event Filter : int;

machine Voldchain {
  var chain : seq[int];
  var msg : map[int, (ver : int, val : int, cli : int)];
  var db : map[int, (ver : int, val : int, cli : int)];
  var pc : map[int, int];
  var up : map[int, bool];
  var nodes : seq[int];
  var clients : seq[int];
  var STOP : int;
  var FailNum : int;
  var driver : machine;
  var cntr : map[int, int];
  var hver : map[int, int];
  start state Init {
    entry (pld : (driver : machine, N : int, C : int, STOP : int, FAILNUM : int)) {
      var i : int;
      driver = pld.driver;
      FailNum = pld.FAILNUM;
      STOP = pld.STOP;
      while (i < pld.N) {
        nodes += (i, i + 1);
        pc[nodes[i]] = 0;
        up[nodes[i]] = true;
        db[nodes[i]] = (ver=-1, val=-1, cli=-1);
        i = i + 1;
      }
      i = 0;
      while (i < pld.C) {
        clients += (i, i + 1 + pld.N);
        pc[clients[i]] = 0;
        cntr[clients[i]] = 0;
        hver[clients[i]] = -1;
        i = i + 1;
      }
    }

    on eNext do {
      var i : int;
      var j : int;
      var choices : seq[(int, int)];
      var ifBranch : bool;
      var added : bool;
      var next : bool;
      var chainTmp : seq[int];
      var chainTmp2 : seq[int];
      i = 0;
      // node
      while (i < sizeof(nodes)) {
        if (pc[nodes[i]] == 0) {
          // NM
          if ((nodes[i] in msg) && up[nodes[i]] &&
              (nodes[i] in chain)) {
            choices += (sizeof(choices), (nodes[i], 0));
          }
          if ((FailNum > 0 && (up[nodes[i]])) || !up[nodes[i]]) {
            choices += (sizeof(choices), (nodes[i], 2));
          }
        }
        // NEM
        else if (pc[nodes[i]] == 1) {
          // consume
          choices += (sizeof(choices), (nodes[i], 1));
          // option: execute NEM here
          // msg -= nodes[i];
          // pc[nodes[i]] = 0;
        }
        i = i + 1;
      }
      i = 0;
      // client
      while (i < sizeof(clients)) {
        if (pc[clients[i]] == 0) {
          if (sizeof(chain) > 0) {
            pc[clients[i]] = 1;
          }
        }
        if (pc[clients[i]] == 1) {
          if (cntr[clients[i]] <= STOP) {
            pc[clients[i]] = 2;
          }
        }
        if (pc[clients[i]] == 2) {
          if (!(clients[i] in msg)) {
            if (!(chain[sizeof(chain) - 1] in msg)) {
              choices += (sizeof(choices), (clients[i], 3));
            } else if ((msg[chain[sizeof(chain) - 1]].ver != hver[clients[i]]) ||
                (msg[chain[sizeof(chain) - 1]].val != -1) ||
                (msg[chain[sizeof(chain) - 1]].cli != clients[i])) {
              choices += (sizeof(choices), (clients[i], 3));
            }
          } else {
            // execute after CLR loop here
            hver[clients[i]] = msg[clients[i]].ver + 1;
            msg -= clients[i];
            pc[clients[i]] = 3;
            //choices += (sizeof(choices), (clients[i], 4));
          }
        }
        if (pc[clients[i]] == 3) {
          if (!(clients[i] in msg)) {
            if (chain[0] in msg) {
              if (msg[chain[0]].ver != hver[clients[i]] ||
                  msg[chain[0]].val != cntr[clients[i]] ||
                  msg[chain[0]].cli != clients[i]) {
                choices += (sizeof(choices), (clients[i], 5));
              }
            } else {
              choices += (sizeof(choices), (clients[i], 5));
            }
          }
          else if (!((msg[clients[i]].ver == hver[clients[i]]) &&
                     (msg[clients[i]].cli == clients[i]))) {
            if (chain[0] in msg) {
              if (msg[chain[0]].ver != hver[clients[i]] ||
                  msg[chain[0]].val != cntr[clients[i]] ||
                  msg[chain[0]].cli != clients[i]) {
                choices += (sizeof(choices), (clients[i], 5));
              }
            } else {
              choices += (sizeof(choices), (clients[i], 5));
            }
          }
          else {
            // execute after CLW while loop here
            cntr[clients[i]] = cntr[clients[i]] + 1;
            msg -= clients[i];
            pc[clients[i]] = 1;
            next = true;
            //choices += (sizeof(choices), (clients[i], 6));
          }
        }
        i = i + 1;
      }
      // configurator
      ifBranch = false;
      if (sizeof(chain) < 3) {
        i = 0;
        while (i < sizeof(nodes)) {
          if (up[nodes[i]] && !(nodes[i] in chain)) {
            choices += (sizeof(choices), (nodes[i], 7));
            ifBranch = true;
          }
          i = i + 1;
        }
      }
      added = false; 
      if (!ifBranch) {
        i = 0;
        while (!added && (i < sizeof(chain))) {
          if (!up[chain[i]]) {
            choices += (sizeof(choices), (i, 8));
            added = true;
          }
          i = i + 1;
        }
      }
      if (sizeof(choices) > 0) {
        i = choose(sizeof(choices));
        if (choices[i].1 == 0) {
          raise NM, choices[i].0;
        }
        if (choices[i].1 == 1) {
          raise NEM, choices[i].0;
        }
        if (choices[i].1 == 2) {
          raise NDF, choices[i].0;
        }
        if (choices[i].1 == 3) {
          raise CLR, choices[i].0;
        }
        if (choices[i].1 == 4) {
          raise R, choices[i].0;
        }
        if (choices[i].1 == 5) {
          raise CLW, choices[i].0;
        }
        if (choices[i].1 == 6) {
          raise W, choices[i].0;
        }
        if (choices[i].1 == 7) {
          raise AddNode, choices[i].0;
        }
        if (choices[i].1 == 8) {
          raise Filter, 0;
        }
      } else if (next) {
        send driver, eNext;
      }
    }

    on NM do (node : int) {
      var i : int;
      var found : bool;
      var next : int;
      print("NM");
      if(msg[node].val != -1) {
        db[node] = msg[node];
      }
      if (node == chain[sizeof(chain) - 1]) {
        msg[msg[node].cli] = db[node];
      } else {
        while (!found && (i < sizeof(chain) - 1)) {
          if (node == chain[i]) {
            found = true;
            next = chain[i + 1];
          }
          i = i + 1;
        }
        assert(found);
        msg[next] = msg[node];
      }
      pc[node] = 1;
      send driver, eNext;
    }

    on NEM do (node : int) {
      print("NEM");
      msg -= node;
      pc[node] = 0;
      send driver, eNext;
    }

    on NDF do (node : int) {
      print("NDF");
      if(FailNum > 0 && up[node]) {
        up[node] = false;
        FailNum = FailNum - 1;
      } else if (!up[node]) {
        up[node] = true;
        msg -= node;
        FailNum = FailNum + 1;
      }
      pc[node] = 0;
      send driver, eNext;
    }

    on CLR do (client : int) {
      print("CLR");
      msg += (chain[sizeof(chain) - 1], (ver = hver[client], val=-1, cli=client));
      send driver, eNext;
    }

    on R do (client : int) {
      print("CLR after loop");
      hver[client] = msg[client].ver + 1;
      msg -= client;
      pc[client] = 3;
      send driver, eNext;
    }

    on CLW do (client : int) {
      print("CLW");
      msg[chain[0]] = (ver=hver[client], val=cntr[client], cli=client);
      send driver, eNext;
    }

    on W do (client : int) {
      print("CLW after loop");
      cntr[client] = cntr[client] + 1;
      msg -= client;
      pc[client] = 1;
      send driver, eNext;
    }

    on AddNode do (node : int) {
      print("Add");
      chain += (sizeof(chain), node);
      if (sizeof(chain) > 1) {
        db[chain[sizeof(chain) - 1]] = db[chain[sizeof(chain) - 2]];
      }
      send driver, eNext;
    }

    on Filter do (idx : int) {
      var i : int;
      var newChain : seq[int];
      print("Filter");
      i = idx;
      while (i < sizeof(chain)) {
        if (up[chain[i]]) {
          newChain += (sizeof(newChain), chain[i]);
        }
        i = i + 1;
      }
      chain = newChain;
      send driver, eNext;
    }
  }
}

machine Main {
  var voldchain : machine;
  var maxNumRequests : int;
  var numRequests : int;
  start state Init {
    entry {
      maxNumRequests = 10;
      voldchain = new Voldchain((driver=this, N=2, C=1, STOP=1, FAILNUM=1));
      send voldchain, eNext;
    }
    on eNext do {
      numRequests = numRequests + 1;
      if (numRequests >= maxNumRequests) {
          raise halt;
      }
      send voldchain, eNext;
    }
  }
}
