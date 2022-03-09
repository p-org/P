/********************************************************
 * This example explains the usage of foreach enumerator
 * ******************************************************/


 machine Main {
 	var ints: set[int];
    var tups: set[(a:int, b:int)];
    var intq: seq[int];
    var mapI: map[int, int];

 	start state Init {
 		entry {
 		  var iter: int;
 			var sum: int;


      // iterate over a set of integers
 			foreach(iter in ints)
 			{
 				sum = sum + iter;
 			}

      assert sum == 234, "Incorrect sum";

      // iterate over a seq of integers
      sum = 0;
      foreach(iter in mapI)
      {
          sum = sum + iter;
      }

      assert sum == 234, "Incorrect sum";

      // We allow iterating over a set or a seq.
      // Hence to iterate over a map, we can use the `keys` or `values` primitive
      foreach(iter in sum)
      {
          assert iter in mapI, "Key should be in the map";
          mapI[iter] = 0;
      }
      // assert that all values are zero
      foreach(iter in values(mapI))
      {
          assert iter == 0, "All values must be zero!";
      }

      // You can mutate the collection while iterating over it.
      // This is because we are iterating over a clone of the collection.
      // assert that all values are zero
      foreach(iter in keys(mapI))
      {
          if(mapI[iter] == 0)
              mapI -= (iter);
      }

      assert sizeof(mapI) == 0, "There should be no value in the map!";
 		}
 	}
 }

