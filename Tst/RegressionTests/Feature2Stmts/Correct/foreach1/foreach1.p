/********************
 * This example explains the usage of foreach iterator
 * ******************/


 machine Main {
 	var ss: set[int];

 	start state Init {
 		entry {
 			var iter: int;
 			var sum: int;
 			ss += (100);
 			ss += (134);

 			foreach(iter in ss)
 			{
 				sum = sum + iter;
 			}

 			assert sum == 234, "Incorrect sum";
 		}
 	}
 }

