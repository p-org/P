package pobserve.commandline.paramvalidator;

import pobserve.commons.commandline.CmdLineParamValidatorHelper;

import com.beust.jcommander.IParameterValidator;

import static pobserve.config.PObserveConfig.getPObserveConfig;

/**
 * Validates the replay window size parameter
 */
public class ValidateReplayWindow implements IParameterValidator {
    public void validate(String name, String value) {
        int replayWindow = CmdLineParamValidatorHelper.validatePositiveInteger(name, value);
        getPObserveConfig().setReplayWindowSize(replayWindow);
    }
}
