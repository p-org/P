package psymbolic.commandline;

public class PSymbolic {

    public static void main(String[] args) {
        // parse the commandline arguments to create the configuration
        PSymConfiguration config = PSymOptions.ParseCommandlineArgs(args);
    }
}
