package pobserve.commandline.paramvalidator;

import pobserve.commons.commandline.CmdLineParamValidatorHelper;
import pobserve.logger.PObserveLogger;

import com.beust.jcommander.IParameterValidator;
import com.beust.jcommander.ParameterException;
import java.io.BufferedReader;
import java.io.FileReader;
import java.io.IOException;
import java.nio.charset.StandardCharsets;

import static pobserve.config.PObserveConfig.getPObserveConfig;

/**
 * ValidateKeyListFile validates the keyListFile param
 */
public class ValidateKeyListFile implements IParameterValidator {
    @Override
    public void validate(String name, String value) throws ParameterException {
        CmdLineParamValidatorHelper.validateFilePath(name, value);
        FileReader fileReader = null;
        BufferedReader bufferedReader = null;
        try {
            fileReader = new FileReader(value, StandardCharsets.UTF_8);
            bufferedReader = new BufferedReader(fileReader);
            String line = null;
            while ((line = bufferedReader.readLine()) != null) {
                getPObserveConfig().getKeys().add(line.strip());
            }
        } catch (Exception e) {
            PObserveLogger.error("Exception occurred while reading keyListFile(" + value + ") ::");
            PObserveLogger.error(e.getMessage());
        } finally {
            try {
                if (bufferedReader != null) {
                    bufferedReader.close();
                }
                if (fileReader != null) {
                    fileReader.close();
                }
            } catch (IOException e) {
                PObserveLogger.error("Exception occurred while closing file(" + value + ") ::");
                PObserveLogger.error(e.getMessage());
            }
        }
    }
}
