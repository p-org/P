A P program is a collection of event and machine declarations.
Here is a basic P program containing a Client machine and a Server machine
communicating with each other via Ping and Pong events.

```linenums="1"
// PingPong.p
event PING assert 1: machine;
event PONG assert 1;
event SUCCESS;

main machine Client {
    var server: machine;

    start state Init {
        entry {
  	        server = new Server();
	        raise SUCCESS;   	   
        }
        on SUCCESS goto SendPing;
    }

    state SendPing {
        entry {
	          send server, PING, this;
	          raise SUCCESS;
	}
        on SUCCESS goto WaitPong;
    }

    state WaitPong {
        on PONG goto SendPing;
    }
}

machine Server {
    start state WaitPing {
        on PING goto SendPong;
    }

    state SendPong {
	      entry (payload: machine) {
	          send payload, PONG;
	          raise SUCCESS;		 	  
	      }
        on SUCCESS goto WaitPing;
    }
}
```

We now take a closer look at the event and machine declarations in this program.
The declaration of event `PING` indicates that it is accompanied with
a data value of `machine` type.
The `machine` type contains the addresses of all dynamically-created
P machines.
This declaration also indicates that at most one instance of the `PING`
event may be present in the input queue of any machine.

A machine declaration contains a collection of variable and state
declarations.
For example, the machine `Client` has a variable `server` and three
states, `Init`, `SendPing`, and `WaitPong`, declared inside it.
The storage for the `server` variable is local to a particular instance
of the `Client` machine.
State `Init` is declared to be the start state to indicate that
an instance of `Client` begins executing by entering the `Init` state.

There can be many different declarations inside a state.
A code block indicated by entry { ... } is executed when
the state is entered.
Similarly, a code block indicated by exit { ... }
(not used in our example) is executed when the state is exited.
Other than these two declarations, all other declarations inside a
state are related to event handling.
The declaration `on SUCCESS goto SendPing` in state `Init` is an
example of such a declaration indicating that the `SUCCESS` event
must be handled by moving to state `SendPing`.
Different states can choose to handle a particular event
differently.
For example, state `Init` handles `SUCCESS` by moving to state `SendPing`
but state `SendPing` handled `SUCCESS` by moving to state `WaitPong`.

Next, we look at the contents of the entry code block in state `Init`
of machine `Client`.
This code block has two statements.
The first statement creates an
instance of the Server machine and stores the address of this
instance in variable `server`.
The second statement raises an event `SUCCESS`
which causes control to exit `Init` and enter `SendPing`.
In P, when a machine sends an event to another machine (or itself),
the event is enqueued in the target machine's input queue.
However, a raised event does not go through the queue;
rather it terminates execution of the enclosing code block and is
handled immediately.

The entry code block in state `SendPing` shows an example of the "send"
statement for sending a `PING` event to the machine whose address
is stored in the variable server.
The keyword `this` evaluates to the address of the machine
executing the current statement.

Finally, the entry code block in state `SendPong` of machine `Server`
shows that the data value associated with a received event can be
retrieved through a parameter of the apppriate type to the event handler.
In this code block, the `payload` parameter evaluates to the data attached to the
`PING` event whose handling caused control to enter `SendPong`.
This data value is the address of the instance of the `Client` machine
that sent the `PING` event.
Similarly, a parameter can be used to retrieve the argument
to the constructor of a freshly-created machine when it starts execution
in the entry block of its `start` state.

The attentive reader might be wondering how the PingPong program
begins execution.
The machine `Client` is annotated with `main` to indicate that program
execution begins with a single instance of `Client` entering state `Init`.
Let us call this `Client` instance X.
X creates an instance of `Server` and raises `SUCCESS` to enter state
`SendPing`.
Let us call this `Server` instance Y; it begins execution in state `WaitPing`
of `Server`.
From this point on, X and Y exchange `PING` and `PONG` messages, where X
cycles between `SendPing` and `WaitPong` and Y cycles between `WaitPing` and
`SendPong`.

The most important safety specification of a P program is that every event
dequeued by a machine is handled; otherwise, the P runtime throws an "UnhandledEvent" exception.
The PingPong program satisfies this specification, since the `Server`
machine handles the `PING` event, and the `Client` machine handles
the `PONG` event in every state where an event dequeue is possible.

In order to terminate a state machine cleanly, there is a special `halt` event.
Termination of a machine due to an unhandled `halt` event is a valid behavior
and does not throw the UnhandledEvent exception.
Therefore, a machine can be halted by queuing a `halt` event to it.
From the point of view of formal operational semantics,
a halted machine is fully receptive and consumes any event that is sent to it.
The P runtime implements this semantics efficiently by cleaning up resources
allocated with the halting machine and recording that the machine has halted.
An event sent to a halted machine is simply dropped.
A halted machine cannot be restarted; it remains halted forever. 