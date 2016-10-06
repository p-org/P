event dummy;

machine Main {
	var x: int;
	start state InitPingPong {
		//both entry and exit are dummy anon funs
		entry {}
		//TODO: if uncommented, diff anon fun names are generated
		//Why? How is "//exit{}" different from no exit declared at all?
		//exit {}
	}

	fun foo() {
		if($)
			x = x + 1;
	}
	fun bar () {
		if($)
			x = x - 1;
	}
	state Fail {
		//both entry and exit are non-dummy named funs
		entry  foo;
		exit bar;
	}

	state Success {
		//entry is non-dummy anon, exit is non-dummy named fun
		entry { 
			send this, dummy;
		}
		exit foo;
	}
	state NewState {
	//both entry and exit are dummy anon funs
	entry {}
	//exit {}
}
}