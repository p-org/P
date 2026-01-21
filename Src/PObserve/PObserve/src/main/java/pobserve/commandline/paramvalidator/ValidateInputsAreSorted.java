package pobserve.commandline.paramvalidator;

import com.beust.jcommander.IParameterValidator;

import static pobserve.config.PObserveConfig.getPObserveConfig;

public class ValidateInputsAreSorted implements IParameterValidator {
    public void validate(String name, String value) {
        getPObserveConfig().setAssumeInputFilesAreSorted(true);
    }
}
