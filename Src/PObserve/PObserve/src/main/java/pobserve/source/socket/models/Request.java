package pobserve.source.socket.models;

import java.util.Map;

/**
 * JSON request object representing the command from the client.
 */
public class Request {
    private String command;
    private String logLine;
    private Map<String, Object> params;

    // Default constructor for Jackson
    public Request() {}

    public Request(String command, String logLine, Map<String, Object> params) {
        this.command = command;
        this.logLine = logLine;
        this.params = params;
    }

    public String getCommand() {
        return command;
    }

    public void setCommand(String command) {
        this.command = command;
    }

    public String getLogLine() {
        return logLine;
    }

    public void setLogLine(String logLine) {
        this.logLine = logLine;
    }

    public Map<String, Object> getParams() {
        return params;
    }

    public void setParams(Map<String, Object> params) {
        this.params = params;
    }
}
