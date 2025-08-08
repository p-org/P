package pobserve.commandline.paramvalidator;

import com.beust.jcommander.IParameterValidator;
import com.beust.jcommander.ParameterException;

import static pobserve.config.PObserveConfig.getPObserveConfig;

/**
 * Validator for host parameter
 * Also updates the socket host in the config
 */
public class ValidateHost implements IParameterValidator {
    @Override
    public void validate(String name, String value) throws ParameterException {
        if (value == null || value.trim().isEmpty()) {
            throw new ParameterException("Host cannot be empty");
        }

        // Update the host in the config
        getPObserveConfig().setHost(value);
    }
}
