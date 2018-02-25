module Elevator
{
  Elevator,
  Door,
  Timer
}

module User 
{
  Main
}

implementation (compose Elevator, User);

//keep adding test cases here
test testcase1: (compose Elevator, User);