package pobserve.commons.commandline;

import com.beust.jcommander.ParameterException;
import java.io.File;
import java.nio.charset.StandardCharsets;
import java.time.Instant;
import java.time.OffsetDateTime;
import java.time.format.DateTimeFormatter;
import java.time.format.DateTimeFormatterBuilder;
import java.time.format.DateTimeParseException;
import java.time.format.ResolverStyle;
import java.time.temporal.ChronoField;
import java.util.regex.Pattern;
import software.amazon.awssdk.regions.Region;

/*
 * CmdLineParamValidatorHelper class has common validator functions to validate commandline params
 * */
public final class CmdLineParamValidatorHelper {

    private CmdLineParamValidatorHelper() {
        throw new UnsupportedOperationException("This is a utility class and cannot be instantiated");
    }

    public static void validateClassname(String name, String value) throws ParameterException {
        Pattern pattern = Pattern.compile("[^a-zA-Z0-9_]");
        if (pattern.matcher(value).find()) {
            throw new ParameterException("The specified class name " + name.replaceFirst("^-*", "")
                    + " (" + value + ") is invalid.");
        }
    }

    public static void validateString(String name, String value) throws ParameterException {
        try {
            byte[] valueInBytes = value.getBytes(StandardCharsets.UTF_8);
            if (valueInBytes.length == 0) {
                throw new ParameterException("The specified " + name.replaceFirst("^-*", "")
                        + " (" + value + ") is invalid. (must not be empty)");
            }
        } catch (Exception e) {
            throw new ParameterException("The specified " + name.replaceFirst("^-*", "")
                    + " (" + value + ") is invalid. (Must contain utf-8 characters only)", e);
        }
    }

    public static void validateFilePath(String name, String value) throws ParameterException {
        File file = new File(value);
        if (!file.exists()) {
            throw new ParameterException("The specified " + name.replaceFirst("^-*", "") + "("
                    + value + ") does not exist.");
        }
        if (!file.isFile()) {
            throw new ParameterException("The specified " + name.replaceFirst("^-*", "")
                    + "(" + value + ") is not a file.");
        }
    }

    public static int validatePositiveInteger(String name, String value) throws ParameterException {
        Pattern pattern = Pattern.compile("[0-9]+");
        if (!pattern.matcher(value).matches()) {
            throw new ParameterException("The specified " + name.replaceFirst("^-*", "") + " (" + value + ") "
                    + "is not valid (Must be an integer > 0)");
        }

        int integerVal = Integer.parseInt(value);
        if (integerVal < 1) {
            throw new ParameterException("The specified " + name.replaceFirst("^-*", "") + " (" + value + ") "
                    + "is not valid (Must be an integer > 0)");
        }
        return integerVal;
    }

    public static void validateAWSRegion(String name, String value) throws ParameterException {
        for (Region region: Region.regions()) {
            if (region.id().equalsIgnoreCase(value)) {
                return;
            }
        }
        throw new ParameterException("The specified " + name.replaceFirst("^-*", "") + " (" + value + ") "
                + "is not valid. Please provide a valid AWS region.");
    }

    public static Instant validateInstantTime(String name, String value) throws ParameterException {
        DateTimeFormatter formatter = new DateTimeFormatterBuilder()
                .appendPattern("uuuu-MM-dd'T'HH:mm:ss")
                .appendLiteral('.')
                .appendValue(ChronoField.MILLI_OF_SECOND, 3)
                .appendOffsetId()
                .toFormatter()
                .withResolverStyle(ResolverStyle.STRICT);
        try {
            OffsetDateTime.parse(value, formatter);
        } catch (DateTimeParseException e) {
            throw new ParameterException("The specified " + name.replaceFirst("^-*", "") + "(" + value
                    + ") is not a valid time. It must be a valid DateTime in the format: yyyy-MM-ddTHH:mm:ss.SSSZ", e);
        }

        Instant time = Instant.parse(value);
        Instant now = Instant.now();
        if (time.isAfter(now)) {
            throw new ParameterException("The specified " + name.replaceFirst("^-*", "") + "("
                    + value + ") is not a valid time. It must be before the current time: " + now.toString());
        }
        return time;
    }
}
