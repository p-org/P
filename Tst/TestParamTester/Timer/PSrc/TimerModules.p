/* Create the timer module which consists of only the timer machine */
module Timer = { Timer };

param i: int;

machine TestWithSingleClient
{
  start state Init {
    entry {
        CreateTimer(this);
    }
  }
}
  
test param (i in [2, 3, 4]) tcSingleClient [main=TestWithSingleClient]:
  (union Timer, { TestWithSingleClient });