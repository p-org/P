package pobserve.commons.logging;

import java.util.Map;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.ObjectWriter;

import lombok.Builder;
import lombok.Getter;
import software.amazon.awssdk.annotations.NotNull;

//  This class generates Strings in JSON format.
@Builder
@Getter
@JsonInclude(JsonInclude.Include.NON_ABSENT)
public class LogObject {
    @NotNull
    private int statusCode;
    @NotNull
    private String message;

    private Exception exception;
    private Map<String, String> additionalInfo;

    /**
     * In-progress logs
     * Logs while the function is still occurring.
     * Returns status code 102.
     *
     * @param message        is the message that is added to the log.
     * @param additionalInfo is a Map of any additional fields to add to the log.
     * @return String representation of JSON log.
     */
    public static String getInProgressLog(String message, Map<String, String> additionalInfo) {
        LogObject obj = LogObject.builder()
                .statusCode(102)
                .message(message)
                .additionalInfo(additionalInfo)
                .build();
        ObjectWriter ow = new ObjectMapper().writer().withDefaultPrettyPrinter();

        try {
            return ow.writeValueAsString(obj);
        } catch (JsonProcessingException e) {
            return e.getMessage();
        }
    }

    /**
     * Success exit log
     * Logs once the function has successfully completed.
     * Returns status code 200.
     *
     * @param message        is the message that is added to the log.
     * @param additionalInfo is a Map of any additional fields to add to the log.
     * @return String representation of JSON log.
     */
    public static String getSuccessLog(String message, Map<String, String> additionalInfo) {
        LogObject obj = LogObject.builder()
                .statusCode(200)
                .message(message)
                .additionalInfo(additionalInfo)
                .build();
        ObjectWriter ow = new ObjectMapper().writer().withDefaultPrettyPrinter();
        try {
            return ow.writeValueAsString(obj);
        } catch (JsonProcessingException e) {
            return e.getMessage();
        }
    }

    /**
     * Error exit log
     * Logs once the function has failed with an error.
     * Returns status code 400.
     *
     * @param message        is the message that is added to the log.
     * @param exception      is the exception that caused the failure and is added
     *                       to the log.
     * @param additionalInfo is a Map of any additional fields to add to the log.
     * @return String representation of JSON log.
     */
    public static String getErrorLog(String message, Exception exception,
            Map<String, String> additionalInfo) {
        LogObject obj = LogObject.builder()
                .statusCode(400)
                .message(message)
                .exception(exception)
                .additionalInfo(additionalInfo)
                .build();
        try {
            return new ObjectMapper().writer().withDefaultPrettyPrinter().writeValueAsString(obj);
        } catch (JsonProcessingException e) {
            return e.getMessage();
        }
    }
}
