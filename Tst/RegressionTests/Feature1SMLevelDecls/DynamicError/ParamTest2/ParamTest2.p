param g1 : int;
param g2 : int;


machine Main {
	var M: machine;
	start state Init {
		entry {
			print format("global varaible g1 = {0}", g1);
      print format("global varaible g2 = {0}", g2);
			assert g1 != 1 && g2 != 3 ;
		}
	}
}


test param (g1 in [1], g2 in [3]) TestParam [main=Main]:{Main};