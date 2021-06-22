package psymbolic.runtime.logger;

import org.apache.log4j.*;
import psymbolic.runtime.machine.Machine;
import psymbolic.runtime.machine.Message;
import psymbolic.runtime.machine.State;
import psymbolic.valuesummary.Guard;
import psymbolic.valuesummary.PrimitiveVS;

import java.io.IOException;
import java.text.SimpleDateFormat;
import java.util.Date;

/**
 * Represents the trace logger for P Symbolic
 */
public class TraceSymLogger extends PSymLogger {

    static Logger log = Logger.getLogger(TraceSymLogger.class.getName());
    
    public static void Initialize()
    {
        // remove all the appenders
        log.removeAllAppenders();
        // setting up the logger
        //This is the root logger provided by log4j
        log.setLevel(Level.DEBUG);

        //Define log pattern layout
        PatternLayout layout = new PatternLayout("%m%n");

        //Add console appender to root logger
        log.addAppender(new ConsoleAppender(layout));

        try
        {
            // get new file name
            SimpleDateFormat formatter = new SimpleDateFormat("dd:MM:yyyy HH:mm:ss");
            Date date = new Date();
            String fileName = "trace-"+date.toString() + ".log";
            //Define file appender with layout and output log file name
            RollingFileAppender fileAppender = new RollingFileAppender(layout, fileName);
            //Add the appender to root logger
            log.addAppender(fileAppender);
        }
        catch (IOException e)
        {
            PSymLogger.error("Failed to add appender to the TraceLogger!!");
        }
    }

    public static void onProcessEvent(Guard pc, Machine machine, Message message)
    {
        String msg = String.format("Machine %s is processing event %s in state %s", machine, message.getEvent(), machine.getState().restrict(pc));
        log.info(msg);
    }

    public static void onProcessStateTransition(Guard pc, Machine machine, PrimitiveVS<State> newState) {
        String msg = String.format("Machine %s transitioning to state %s", machine.toString(), newState);
        log.info(msg);
    }

    public static void onCreateMachine(Guard pc, Machine machine) {
        String msg = "Machine " + machine + " was created";
        log.info(msg);
    }

    public static void onMachineStart(Guard pc, Machine machine) {
        String msg = String.format("Machine %s starting", machine.toString());
        log.info(msg);
    }

    public static void machineState(Guard pc, Machine machine) {
        String msg = String.format("Machine %s in state %s", machine, machine.getState().restrict(pc));
        log.info(msg);
    }

    public static void finished(int steps) {
        log.info(String.format("Execution finished in %d steps", steps));
    }

    public static void handle(Machine m, State st, Message event) {
        log.info("Machine " + m + " handling event " + event.getEvent() + " in state " + st);
    }

    public static void send(Message effect) {
        String msg = "Send effect " + effect.getEvent() + " to " + effect.getTarget();
        log.info(msg);
    }

    public static void schedule(int step, Message effect) {
        String msg = "Step " + step + ": scheduled " + effect.toString() + " sent to " + effect.getTarget();
        log.info(msg);
    }

    public static void logMessage(String str) {
        log.info(str);
    }

    public static void enable() {
        log.setLevel(Level.ALL);
    }

    public static void disable() {
        log.setLevel(Level.OFF);
    }
}
