event eNext;
event H : int;
event P : int;
event E : int;
event T : int;
event R : int;

machine DiningRound0 {
  var driver : machine;
  var N : int;
  var st : map[int, int];
  var pc : map[int, int];
  var fork : map[int, int];
  start state Init {
    entry (pld : (driver : machine, N : int)) {
      var i : int;
      driver = pld.driver;
      N = pld.N;
      while (i < N) {
        st += (i, 0);
        pc += (i, 0);
        fork += (i, N);
        i = i + 1;
      }
    }
    on eNext do {
      var i : int;
      var RightF : int;
      var LeftF : int;
      var choices : seq[(int, int)];
      while (i < N) {
        if (st[i] == 0) {
          if (pc[i] == 0) {
            choices += (sizeof(choices), (i, 0));
          } else {
            choices += (sizeof(choices), (i, 4));
          }
        }
        if (st[i] == 1) {
          if (pc[i] == 0) {
            if (i == 0) {
              RightF = N - 1;
            } else {
              RightF = i - 1;
            }
            if (fork[RightF] == N) {
              choices += (sizeof(choices), (i, 1));
            }
          } else if (pc[i] == 1) {
            LeftF = i;
            if (fork[LeftF] == N) {
              choices += (sizeof(choices), (i, 2));
            }
          }
        }
        if (st[i] == 2) {
          if (pc[i] == 0) {
            choices += (sizeof(choices), (i, 3));
          }
        }
        i = i + 1; 
      }
      assert (sizeof(choices) > 0);
      i = choose(sizeof(choices));
      if (choices[i].1 == 0) {
        raise H, choices[i].0;
      }
      if (choices[i].1 == 1) {
        raise P, choices[i].0;
      }
      if (choices[i].1 == 2) {
        raise E, choices[i].0;
      }
      if (choices[i].1 == 3) {
        raise T, choices[i].0;
      }
      if (choices[i].1 == 4) {
        raise R, choices[i].0;
      }
    }
    on H do (proc : int) {
      st[proc] = 1;
      send driver, eNext;
    }
    on P do (proc : int) {
      var RightF : int;
      if (proc == 0) {
        RightF = N - 1;
      } else {
        RightF = proc - 1;
      }
      fork[RightF] = proc;
      pc[proc] = 1;
      send driver, eNext;
    }
    on E do (proc : int) {
      var LeftF : int;
      LeftF = proc;
      fork[LeftF] = proc;
      st[proc] = 2;
      pc[proc] = 0;
      send driver, eNext;
    }
    on T do (proc : int) {
      var LeftF : int;
      LeftF = proc;
      st[proc] = 0;
      fork[LeftF] = N;
      pc[proc] = 1;
      send driver, eNext;
    }
    on R do (proc : int) {
      var RightF : int;
      if (proc == 0) {
        RightF = N - 1;
      } else {
        RightF = proc - 1;
      }
      fork[RightF] = N;
      pc[proc] = 0;
      send driver, eNext;
    }
  }
}

machine Main {
  var dining : machine;
  start state Init {
    entry {
      dining = new DiningRound0((driver=this, N=2));
      send dining, eNext;
    }
    on eNext do {
      send dining, eNext;
    }
  }
}
