package pobserve.commandline.paramvalidator;

import pobserve.commons.commandline.CmdLineParamValidatorHelper;

import com.beust.jcommander.IParameterValidator;
import com.beust.jcommander.ParameterException;

import static pobserve.config.PObserveConfig.getPObserveConfig;


/**
 * ValidateSpecName checks the spec name parameter
 */
public class ValidateSpecName implements IParameterValidator {
    public void validate(String name, String value)
            throws ParameterException {
        CmdLineParamValidatorHelper.validateClassname(name, value);
        getPObserveConfig().setSpecificationName(value);
    }
}
