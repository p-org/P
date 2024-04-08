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
            var iterTup: (a:int, b:int);
 			var sum: int;

            // initialize the data structures
 			Initialize();

            // iterate over a set of integers
 			foreach(iter in ints)
 			{
 				sum = sum + iter;
 			}

            assert sum == 234, "Incorrect sum";
            // iterate over a set of tuples
            foreach(iterTup in tups)
            {
                assert iterTup.a < iterTup.b, "Incorrect entries in tuples";
            }

            // iterate over a seq of integers
            sum = 0;
            foreach(iter in intq)
            {
                sum = sum + iter;
            }

            assert sum == 234, "Incorrect sum";

            // We allow iterating over a set or a seq.
            // Hence to iterate over a map, we can use the `keys` or `values` primitive
            foreach(iter in keys(mapI))
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

    fun Initialize() {
        ints += (100);
        ints += (134);

        tups += ((a = 1, b = 4));
        tups += ((a=3, b=100));
        tups += ((a=4, b=44));

        intq += (0, 130);
        intq += (1, 4);
        intq += (2, 100);

        mapI += (100, 4);
        mapI += (23, 44);
        mapI += (55, 2222);
        mapI += (4, 66);
    }
 }

