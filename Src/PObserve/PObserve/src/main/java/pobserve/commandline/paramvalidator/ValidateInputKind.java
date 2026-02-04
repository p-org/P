package pobserve.commandline.paramvalidator;

import pobserve.config.SourceInputKind;

import com.beust.jcommander.IParameterValidator;
import com.beust.jcommander.ParameterException;

import static pobserve.config.PObserveConfig.getPObserveConfig;

public class ValidateInputKind implements IParameterValidator {
    public void validate(String name, String value) {
        boolean isValidSourceKind = false;
        for (SourceInputKind kind : SourceInputKind.values()) {
            if (kind.name().equalsIgnoreCase(value)) {
                isValidSourceKind = true;
                getPObserveConfig().setInputKind(kind);
                break;
            }
        }
        if (!isValidSourceKind) {
            throw new ParameterException("The specified input kind (" + value + ") is not valid.");
        }
    }
}
