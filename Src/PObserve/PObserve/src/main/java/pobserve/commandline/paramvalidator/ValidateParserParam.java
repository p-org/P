package pobserve.commandline.paramvalidator;

import pobserve.commons.commandline.CmdLineParamValidatorHelper;

import com.beust.jcommander.IParameterValidator;
import com.beust.jcommander.ParameterException;

import static pobserve.config.PObserveConfig.getPObserveConfig;

public class ValidateParserParam implements IParameterValidator {
    public void validate(String name, String value)
            throws ParameterException {
        CmdLineParamValidatorHelper.validateClassname(name, value);
        getPObserveConfig().setParserName(value);
    }
}
