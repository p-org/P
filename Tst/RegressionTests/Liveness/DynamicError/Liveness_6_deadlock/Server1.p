event Local;
event Search;
event SearchStarted;
event SearchFinished;

machine Main
{
   var store : seq[int];

   start state Init
   {
      entry
      {
         send  this, SearchStarted;
         receive {
         	case Search: {}
         }
      }
      ignore SearchStarted;
   }
}

spec Liveness observes SearchStarted, SearchFinished
{
   start cold state Searched
   {
      on SearchStarted goto Searching;
   }

   hot state Searching
   {
      on SearchFinished goto Searched;
      on SearchStarted goto Searching;
   }
}
