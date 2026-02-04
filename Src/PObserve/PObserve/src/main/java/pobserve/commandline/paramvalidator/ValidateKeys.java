package pobserve.commandline.paramvalidator;

import com.beust.jcommander.IParameterValidator;
import com.beust.jcommander.ParameterException;

import static pobserve.config.PObserveConfig.getPObserveConfig;

/**
 * ValidateKeys checks the keys parameter
 */
public class ValidateKeys implements IParameterValidator {
    public void validate(String name, String value)
            throws ParameterException {
        getPObserveConfig().getKeys().add(value);
    }
}
