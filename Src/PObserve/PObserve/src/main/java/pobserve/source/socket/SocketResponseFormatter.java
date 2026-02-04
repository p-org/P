package pobserve.source.socket;

import pobserve.logger.PObserveLogger;
import pobserve.source.socket.models.Response;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import java.io.PrintWriter;
import java.util.List;
import java.util.Map;

/**
 * Handles formatting and sending responses to socket clients.
 * Supports both JSON and TEXT formats.
 */
public class SocketResponseFormatter {

    private final ObjectMapper objectMapper;

    // Command constants
    private static final String CMD_START = "START";
    private static final String CMD_RESET = "RESET";
    private static final String CMD_ACCEPT = "ACCEPT";
    private static final String CMD_STOP = "STOP";

    /**
     * Creates a new SocketResponseFormatter with the provided ObjectMapper.
     *
     * @param objectMapper The ObjectMapper to use for JSON serialization
     */
    public SocketResponseFormatter(ObjectMapper objectMapper) {
        this.objectMapper = objectMapper;
    }

    /**
     * Send a response to the client in the appropriate format (JSON or TEXT).
     *
     * @param writer The output writer
     * @param isJsonMode Whether to use JSON or TEXT format for the response
     * @param command The command that was executed (may be null)
     * @param message The message to send
     * @param data Additional data for the response (may be null)
     * @param success Whether the command succeeded
     */
    public void sendResponse(PrintWriter writer, boolean isJsonMode, String command,
                             String message, Map<String, Object> data, boolean success) {
        if (isJsonMode) {
            sendJsonResponse(writer, success, message, data);
        } else {
            sendTextResponse(writer, command, success, message, data);
        }
    }

    /**
     * Sends a JSON formatted response.
     */
    private void sendJsonResponse(PrintWriter writer, boolean success,
                                 String message, Map<String, Object> data) {
        // JSON format
        Response response = new Response(success ? "SUCCESS" : "ERROR", message, data);
        try {
            writer.println(objectMapper.writeValueAsString(response));
        } catch (JsonProcessingException e) {
            PObserveLogger.error("Error serializing response to JSON: " + e.getMessage());
            writer.println("{\"status\":\"ERROR\",\"message\":\"Error serializing response\"}");
        }
    }

    /**
     * Sends a TEXT formatted response.
     */
    private void sendTextResponse(PrintWriter writer, String command, boolean success,
                                 String message, Map<String, Object> data) {
        if (CMD_STOP.equals(command) && success && data != null) {
            // For STOP command, provide more detailed output in TEXT mode
            StringBuilder responseText = new StringBuilder();
            responseText.append("SUCCESS|").append(message).append("\n");
            responseText.append("PObserve run completed with ").append(data.get("errorCount")).append(" error(s)\n");

            // Add metrics
            responseText.append("Metrics:\n");
            @SuppressWarnings("unchecked")
            Map<String, Integer> metrics = (Map<String, Integer>) data.get("metrics");
            if (metrics != null) {
                for (Map.Entry<String, Integer> entry : metrics.entrySet()) {
                    responseText.append("- ").append(entry.getKey()).append(": ").append(entry.getValue()).append("\n");
                }
            }

            // Add execution time
            responseText.append("Execution time: ").append(data.get("executionTimeMs")).append("ms\n");

            // Add errors if any
            @SuppressWarnings("unchecked")
            List<String> errors = (List<String>) data.get("errors");
            if (errors != null && !errors.isEmpty()) {
                responseText.append("Errors:\n");
                for (String error : errors) {
                    responseText.append("- ").append(error).append("\n");
                }
            }

            writer.println(responseText.toString());
        } else {
            // For other commands, just use status and message
            writer.println((success ? "SUCCESS|" : "ERROR|") + message);
        }
    }
}
