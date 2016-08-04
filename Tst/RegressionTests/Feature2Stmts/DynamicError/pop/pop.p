// This sample tests pop inside a top-level entry function.

machine Main {
	start state Init {
		entry {
			pop;
		}
	}
}

