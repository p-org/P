# P to RV-Monitor translation

## Background

RV-Monitor is a tool that instruments Java programs, monitoring that various
user-defined properties hold at runtime.

## Short usage description

The `Tst/RegressionTests/Rvm/Example` folder contains working examples that
you can run, e.g.:
```
cd Tst/RegressionTests/Rvm/Example/twophasecommit
python run_test.py
```

### Step-by-step usage

To generate RVM files from a P specification (let's call it `spec.p`), run
```
dotnet Bld/Drops/Release/Binaries/Pc.dll spec.p -g:RVM
```

This will generate a `rvm` file, an `aj` file and one or more `java` files.
The `rvm` and `java` files implement a monitor, as described
in the P specification. The `aj` file contains the code that links your Java
code with that monitor.

Currently there is no automatic way to link P events to Java code, so
you should edit the `aj` file to add this glue code. As an example, see the
`Tst/RegressionTests/Rvm/Example/twophasecommit/monitor/ajcode.txt` file,
which is inserted by the `run_test.py` script in the generated `aj` file,
replacing the `// Implement your code here.` comment.

Next, you should run the `rv-monitor` tool on the `rvm` file to transform it
into a Java class, e.g.
```
rv-monitor -merge spec.rvm
```

Next, you should create a package directory called `mop` in your project,
containing all the generated Java files and the Java files in
`Src/PRuntimes/RvmRuntime`.

Next, run the AspectJ compiler with the `aj` file and your project as arguments.

## Detailed explanations.

The generated code consists of:

* A `StateBase.java` class, which contains the base class for all P states. It
  has a default implementation for each possible event, currently printing a
  warning message. It also has empty implementations for the `entry` and
  `exit` functions.
  ```
    abstract class StateBase implements IState {
        @Override
        public void entry(Optional<Object> maybePayload)
            throws GotoStmtException, RaiseStmtException {
        }
        
        @Override
        public void exit()
            throws GotoStmtException, RaiseStmtException {
        }
        
        public void event1Handler()
            throws GotoStmtException, RaiseStmtException {
          System.out.println(
              "No event handler for state '" + getName() + "' and event 'event1'.");
        }
        
    }
  ```
* An `Events.java` file, containing definitions for all events. An event
  consists of a handler which calls a state's event handler, e.g.
  ```
      static class Event1Event implements IEvent<StateBase> {
        @Override
        public void handle(StateBase state, Optional<Object> maybePayload)
            throws GotoStmtException, RaiseStmtException {
          state.event1Handler();
        }
    }
  ```
* A `rvm` file, containing the actual monitor. This is organized in several
  sections (some details are missing)
  1. For each state there is a class extending `StateBase`, implementing
    that state's handlers. An event handler just calls the event handler defined
    in the containing rvm spec e.g.
      ```
      private class InitState extends StateBase {
          @Override
          public String getName() { return "Init"; }
          
          @Override
          public void event1Handler()
              throws GotoStmtException, RaiseStmtException {
            Init_event1();
          }
      }
      ```
  1. For each event in a state, there is a method with the actual handler
    implementation, e.g.
      ```
      private void Init_event1() throws GotoStmtException, RaiseStmtException {
        IEvent TMP_tmp0 = null;
        TMP_tmp0 = new Events.Event2Event();
        throw new RaiseStmtException(TMP_tmp0, Optional.empty());
      }
      ```
  1. State changing and `raise` handling functions
  1. Variables declared in the p spec, together with a variable containing the
    current state, e.g.
      ```
      private IntValue participantNum = new IntValue(0L);
      private IntValue preparedNum = new IntValue(0L);
      private IntValue rolledbackNum = new IntValue(0L);
      private IntValue committedNum = new IntValue(0L);
      
      StateBase state = new InitState();
      ```
  1. A constructor, which calls the initial state's entry handler
  1. Event handlers that will be called by the AspectJ glue code, and which call
    the current state event handlers:
      ```
      event event1 () {
        try {
          currentState.getState().event1Handler();
        } catch (GotoStmtException exception) {
          currentState.changeStateTo(
              (StateBase)exception.getState(),
              exception.getPayload());
        } catch (RaiseStmtException exception) {
          currentState.handleRaisedEvent(
              exception.getEvent(), exception.getPayload());
        }
      }
      ```
