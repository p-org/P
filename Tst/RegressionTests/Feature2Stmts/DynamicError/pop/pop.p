// This sample XYZs pop inside a top-level entry function.

machine Main {
	start state Init {
		entry {
			pop;
		}
	}
}

