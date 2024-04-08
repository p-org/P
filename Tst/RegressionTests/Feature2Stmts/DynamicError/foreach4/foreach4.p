/********************
 * This example explains the usage of foreach iterator with break and continue
 * ******************/


 machine Main {
 	var ss: set[int];

 	start state Init {
 		entry {
 			var iter: int;
 			var sum: int;
 			ss += (100);
 			ss += (134);
 			ss += (123);
 			ss += (245);

 			foreach(iter in ss)
 			{
 			    print format ("Iter = {0}, Sum = {1}", iter, sum);
 			    assert sum <= 100, "Incorrect sum inside loop";
 				if (iter == 134) {
 				    continue;
 				}
 				sum = sum + iter;
 				if (iter == 123) {
 				    break;
 				}
 			}

            print format ("Final Sum = {0}", sum);
 			assert sum != 223, "Should get triggered";
 		}
 	}
 }

