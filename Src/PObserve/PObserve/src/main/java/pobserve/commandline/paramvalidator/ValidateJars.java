package pobserve.commandline.paramvalidator;

import pobserve.commons.commandline.CmdLineParamValidatorHelper;

import com.beust.jcommander.IParameterValidator;

import static pobserve.config.PObserveConfig.getPObserveConfig;

public class ValidateJars implements IParameterValidator {
    public void validate(String name, String value) {
        CmdLineParamValidatorHelper.validateFilePath(name, value);
        getPObserveConfig().getSupplierJars().add(value);
    }
}
