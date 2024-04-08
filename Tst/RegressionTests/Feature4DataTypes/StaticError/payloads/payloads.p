event E;
event F: any;
event G: int;

machine Main {
   var x: event;
   var y: int;
   start state S
   {
      entry
      {
         send this, E;
         send this, E, null;
         send this, E, 1;
         send this, E, this;
         send this, E, x;

         send this, F;
         send this, F, null;
         send this, F, 1;
         send this, F, this;
         send this, F, x;

         send this, G;
         send this, G, null;
         send this, G, 1;
         send this, G, this;
         send this, G, x;

         send this, x;
         send this, x, null;
         send this, x, 1;
         send this, x, this;
         send this, x, x;

         send x, E;
         send x, E, null;
         send x, E, 1;
         send x, E, this;
         send x, E, x;

         send this, y+1;
         send this, y+1, null;
         send this, y+1, 1;
         send this, y+1, this;
         send this, y+1, x;

         raise E;
         raise E, null;
         raise E, 1;
         raise E, this;
         raise E, x;
      }
   }
}
