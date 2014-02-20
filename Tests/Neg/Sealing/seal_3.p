// this example checks the multiple delaying scheduler 

event request;
event response;
event local;
event c_init : id;
main machine godMachine {

	start state init{
		entry {
			__seal__rtc();
			__unseal();
		}
	
	}
}

