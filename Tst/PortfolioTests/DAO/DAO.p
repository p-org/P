event eNext;

machine DAO {
  var pc : int;
  var driver : machine;
  var bankBalance : int;
  var amount : int;
  var stackCount : int;
  var malloryBalance : int;
  var attack : int;

  start state Init {
    entry (pld : (driver : machine, BALANCE : int, AMOUNT : int)) {
      pc = 0;
      bankBalance = pld.BALANCE;
      amount = pld.AMOUNT;
      attack = 3;
      driver = pld.driver;
    }
    on eNext do { 
      // withdraw
      if (pc == 0 && (bankBalance >= amount)) {
        pc = 1;
        stackCount = stackCount + 1;
        send driver, eNext;
      }
      else if (pc == 1) {
        // dispense
        malloryBalance = malloryBalance + amount;
        if (attack > 0) {
          attack = attack - 1;
          stackCount = stackCount + 1;
          pc = 0;
        } else {
          pc = 2;
        }
        send driver, eNext;
      }
      else if (pc == 2 && (stackCount > 0)) {
        // update bank balance
        bankBalance = bankBalance - amount;
        stackCount = stackCount - 1;
        send driver, eNext;
      }
    }
  }
}
machine Main {
  var dao : machine;
  start state Init {
    entry {
      dao = new DAO((driver=this, BALANCE=500, AMOUNT=5));
      send dao, eNext;
    }
    on eNext do {
     send dao, eNext;
    }
  }
}
