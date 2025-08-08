package pobserve.commandline.paramvalidator;

import com.beust.jcommander.IParameterValidator;
import com.beust.jcommander.ParameterException;

import static pobserve.config.PObserveConfig.getPObserveConfig;

/**
 * Validator for socket mode parameter.
 * Updates the config with socket mode flag when the --socket-mode option is used.
 */
public class ValidateSocketMode implements IParameterValidator {
    @Override
    public void validate(String name, String value) throws ParameterException {
        // Socket mode is a flag parameter with no value to validate
        // Just set socket mode to true in the config
        getPObserveConfig().setSocketMode(true);

        // Note: Host and port will be set later during parameter processing
        // This validator is only responsible for setting the socketMode flag
    }
}
