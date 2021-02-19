event a;
machine Main {
	start state S {
		entry {
      	send this, a;
      	send this, a;
			receive {
				case a: {}
			}

			receive {
				case a: {}
			}
		}
	}

	fun Foo() {
        if ($) {
            // Some important stuff
        } else {
            Foo();
        }
    }
}