package pobserve.testing;

import java.io.BufferedReader;
import java.io.File;
import java.io.FileNotFoundException;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.net.JarURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.nio.file.DirectoryStream;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.AbstractMap;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Enumeration;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.jar.JarEntry;
import java.util.jar.JarFile;

public abstract class BaseHelperFunctions {
    /**
     * Creates a new array of arguments by updating the baseArgs with the provided key-value pairs.
     *
     * @param keyValues the map containing key-value pairs to update the arguments
     * @param baseArgs the original array of arguments to be updated
     * @return a new array with updated arguments
     */
    protected static String[] createArgs(Map<String, String> keyValues, String[] baseArgs) {
        String[] customArgs = Arrays.copyOf(baseArgs, baseArgs.length);
        for (Map.Entry<String, String> entry : keyValues.entrySet()) {
            String key = entry.getKey();
            String value = entry.getValue();
            for (int i = 0; i < customArgs.length; i++) {
                if (customArgs[i].equals(key)) {
                    customArgs[i + 1] = value;
                    break;
                }
            }
        }
        return customArgs;
    }

    /**
     * getExpectedErrors(String resourcePath)
     * return type: <code>AbstractMap.SimpleEntry&lt;List&lt;Long%gt;, List&lt;String&gt;&gt;</code>
     * This function parses a file in the format "key: value", and returns a list of
     * the two values.
     */
    public static AbstractMap.SimpleEntry<List<Long>, List<String>> getExpectedErrors(String resourcePath)
            throws IOException {
        InputStream inputStream = BaseHelperFunctions.class.getClassLoader().getResourceAsStream(resourcePath);
        if (inputStream == null) {
            throw new FileNotFoundException("Resource not found: " + resourcePath);
        }
        List<Long> errorTimestamps = new ArrayList<>();
        List<String> errorMessages = new ArrayList<>();

        try (BufferedReader reader = new BufferedReader(new InputStreamReader(inputStream, StandardCharsets.UTF_8))) {
            String line;
            while ((line = reader.readLine()) != null) {
                String[] parts = line.split(": ", 2);
                long timestamp = Long.parseLong(parts[0]);
                String message = parts[1];
                errorTimestamps.add(timestamp);
                errorMessages.add(message);
            }
        }
        return new AbstractMap.SimpleEntry<List<Long>, List<String>>(errorTimestamps, errorMessages);
    }

    /**
     * createTempFileFromResource(String path)
     * return type: File
     * - This function takes the path of the log file/directory from the jar. If it
     * is a log file, it will return the path to the temporary log file.
     * if it is a directory of files, it will return the path to the temporary
     * directory of temporary log files.
     */
    protected static String createTempFilesFromResource(String resourcePath) throws IOException {
        // If it is a single file, we return the path to the file
        if (resourcePath.contains(".txt")) {
            InputStream inputStream = BaseHelperFunctions.class.getClassLoader().getResourceAsStream(resourcePath);
            if (inputStream == null) {
                throw new FileNotFoundException("Resource not found: " + resourcePath);
            }
            File tempFile = File.createTempFile("log", ".txt");
            tempFile.deleteOnExit();

            try (FileOutputStream out = new FileOutputStream(tempFile)) {
                byte[] buffer = new byte[1024];
                int bytesRead;
                while ((bytesRead = inputStream.read(buffer)) != -1) {
                    out.write(buffer, 0, bytesRead);
                }
            }
            return tempFile.getAbsolutePath();
        }

        // Else, we return the path to the directory containing the files
        ClassLoader classLoader = BaseHelperFunctions.class.getClassLoader();
        URL dirUrl = classLoader.getResource(resourcePath);

        if (dirUrl == null) {
            throw new FileNotFoundException("Resource not found: " + resourcePath);
        }

        JarURLConnection jarConnection = (JarURLConnection) dirUrl.openConnection();
        JarFile jar = jarConnection.getJarFile();

        Enumeration<JarEntry> entries = jar.entries();

        Path tempDirectory = Files.createTempDirectory("temp_dir_logs_");

        while (entries.hasMoreElements()) {
            JarEntry entry = entries.nextElement();
            String name = entry.getName();
            if (name.startsWith(resourcePath + "/") && !name.equals(resourcePath + "/")) {
                File tempFile = new File(tempDirectory.toFile(), new File(name).getName());
                tempFile.deleteOnExit();

                InputStream inputStream = classLoader.getResourceAsStream(name);
                try (FileOutputStream out = new FileOutputStream(tempFile)) {
                    if (inputStream == null) {
                        throw new FileNotFoundException("Resource not found: " + name);
                    }
                    byte[] buffer = new byte[1024];
                    int bytesRead;
                    while ((bytesRead = inputStream.read(buffer)) != -1) {
                        out.write(buffer, 0, bytesRead);
                    }
                }
            }
        }
        return tempDirectory.toString();
    }

    /**
     * parseParams(String content)
     * return type: <code>Map&lt;String, String&gt;</code>
     * This private function takes the contents of a param.txt file, and returns
     * the arguments in a kv pair.
     */
    protected static Map<String, String> parseParams(String content) {
        Map<String, String> params = new HashMap<>();
        String[] lines = content.split("\n");

        for (String line : lines) {
            String[] keyValue = line.split(":", 2);
            if (keyValue.length == 2) {
                params.put(keyValue[0].trim(), keyValue[1].trim());
            }
        }
        return params;
    }

    /**
     * extractParamsFromTempDir(String resourcePath)
     * return type: <code>Map&lt;String, Map&lt;String, String&gt;&gt;</code>
     * This function takes the path of the params directory, and returns the
     * parameters needed to run PObserve for each test case.
     * format: test_case_x_params.txt={kv pairs}
     */
    protected static Map<String, Map<String, String>> extractParamsFromTempDir(String resourcePath) throws IOException {
        Map<String, Map<String, String>> params = new HashMap<>();
        Path tempDir = Paths.get(resourcePath);

        try (DirectoryStream<Path> stream = Files.newDirectoryStream(tempDir, "*.txt")) {
            for (Path filePath : stream) {
                String content = new String(Files.readAllBytes(filePath), StandardCharsets.UTF_8);
                Map<String, String> parsedParamFields = parseParams(content);
                Path filename = filePath.getFileName();
                if (filename != null) {
                    params.put(filename.toString(), parsedParamFields);
                }
            }
        }
        return params;
    }

    /**
     * extractSubDirs(String resourcePath)
     * return type: <code>Map&lt;String, Integer&gt;</code>
     * This function returns all .txt files and directories one level below the
     * resourcePath.
     * e.g. if path="logs/", this will return "logs/test_case_2.txt" and
     * "logs/test_case_3/"
     */
    protected static Map<String, Integer> extractSubDirs(String resourcePath) throws IOException {
        ClassLoader classLoader = BaseHelperFunctions.class.getClassLoader();
        URL dirUrl = classLoader.getResource(resourcePath);

        if (dirUrl == null) {
            throw new FileNotFoundException("Resource not found: " + resourcePath);
        }

        JarURLConnection jarConnection = (JarURLConnection) dirUrl.openConnection();
        JarFile jar = jarConnection.getJarFile();

        Enumeration<JarEntry> entries = jar.entries();

        Map<String, Integer> subDirs = new HashMap<String, Integer>();

        while (entries.hasMoreElements()) {
            JarEntry entry = entries.nextElement();
            String name = entry.getName();

            long depth = name.chars().filter(ch -> ch == '/').count();

            if (name.startsWith(resourcePath)) {
                if (name.endsWith(".txt") && depth == 3) {
                    subDirs.put(name, 0);
                } else if (name.endsWith("/") && depth == 4) {
                    subDirs.put(name.substring(0, name.length() - 1), 0);
                }
            }
        }
        return subDirs;
    }
}
