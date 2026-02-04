package pobserve.commandline.paramvalidator;

import com.beust.jcommander.IParameterValidator;
import com.beust.jcommander.ParameterException;

import static pobserve.config.PObserveConfig.getPObserveConfig;

/**
 * Validator for port numbers
 * Also updates the socket port in the config
 */
public class ValidatePort implements IParameterValidator {
    @Override
    public void validate(String name, String value) throws ParameterException {
        try {
            int port = Integer.parseInt(value);
            if (port < 1 || port > 65535) {
                throw new ParameterException("Port must be between 1 and 65535");
            }

            // Update the port in the config
            getPObserveConfig().setPort(port);
        } catch (NumberFormatException e) {
            throw new ParameterException("Port must be a valid integer", e);
        }
    }
}
