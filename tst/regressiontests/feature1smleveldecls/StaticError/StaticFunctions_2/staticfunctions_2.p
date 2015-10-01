event a;

static fun F1() {

}

static fun F2() {

}

main machine M {
	start state S {
		entry {
		
		}
		on a do F1;
		on a goto S with F2;
		exit F1;
	}
	
	state S1 {
		entry F2;
	}
}