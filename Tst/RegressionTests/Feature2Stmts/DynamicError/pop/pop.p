// This sample tests pop inside a top-level entry function.

main machine A {
	start state Init {
		entry {
			pop;
		}
	}
}

