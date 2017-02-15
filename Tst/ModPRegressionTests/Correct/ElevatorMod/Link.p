module Elevator 
private;
{
  Elevator,
  Door,
  Timer
}

module User 
private;
{
  Main
}

implementation Elevator || User;

//keep adding test cases here
test testcase1: Elevator || User;
test testcase2: User;