package pobserve.source.socket.models;

import java.util.Map;

/**
 * JSON response object representing the server response.
 */
public class Response {
    private String status;
    private String message;
    private Map<String, Object> data;

    // Default constructor for Jackson
    public Response() {}

    public Response(String status, String message, Map<String, Object> data) {
        this.status = status;
        this.message = message;
        this.data = data;
    }

    public String getStatus() {
        return status;
    }

    public void setStatus(String status) {
        this.status = status;
    }

    public String getMessage() {
        return message;
    }

    public void setMessage(String message) {
        this.message = message;
    }

    public Map<String, Object> getData() {
        return data;
    }

    public void setData(Map<String, Object> data) {
        this.data = data;
    }
}
