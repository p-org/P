package pobserve.commandline.paramvalidator;

import com.beust.jcommander.IParameterValidator;
import com.beust.jcommander.ParameterException;
import java.io.File;
import java.util.ArrayList;
import java.util.List;

import static pobserve.config.PObserveConfig.getPObserveConfig;

public class ValidateLogLocation implements IParameterValidator {
    public void validate(String name, String value)
            throws ParameterException {
        File logLocation = new File(value);
        if (!logLocation.exists()) {
            throw new ParameterException("The specified log location(" + value + ") does not exist.");
        }

        List<File> logFiles = new ArrayList<>();
        if (logLocation.isDirectory()) {
            File[] listOfFiles = logLocation.listFiles();
            assert listOfFiles != null;
            for (File file: listOfFiles) {
                if (file.isFile()) {
                    logFiles.add(file);
                }
            }
        }
        if (logLocation.isFile()) {
            logFiles.add(logLocation);
        }
        getPObserveConfig().setLogFiles(logFiles);
    }
}
