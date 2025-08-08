package pobserve.commandline.paramvalidator;

import com.beust.jcommander.IParameterValidator;
import com.beust.jcommander.ParameterException;
import java.io.File;

import static pobserve.config.PObserveConfig.getPObserveConfig;

public class ValidateOutputDir implements IParameterValidator {
    public void validate(String name, String value)
            throws ParameterException {
        File outputDir = new File(value);

        if (!outputDir.exists()) {
            if (!outputDir.mkdirs()) {
                throw new ParameterException("Failed to create output directory (" + value + ").");
            }
        }
        if (!outputDir.isDirectory()) {
            throw new ParameterException("The specified output directory (" + value + ") is not a directory.");
        }

        getPObserveConfig().setOutputDir(outputDir);
    }
}
