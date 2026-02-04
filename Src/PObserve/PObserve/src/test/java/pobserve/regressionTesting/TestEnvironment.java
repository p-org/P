package pobserve.regressionTesting;

import pobserve.report.TrackErrors;

import java.io.IOException;
import java.nio.file.FileVisitResult;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.nio.file.SimpleFileVisitor;
import java.nio.file.attribute.BasicFileAttributes;
import org.junit.jupiter.api.AfterEach;

public abstract class TestEnvironment {
    /**
     * deleteDirectoryAndContents()
     * - This method is run before each test to clean up the "error_logs" directory.
     * This ensures that each test starts with a clean state.
     */
    @AfterEach
    public void deleteDirectoryAndContents() throws IOException {
        String dirPathStr = PObserveArgsConstants.errorDirPath;
        Path dirPath = Paths.get(dirPathStr);
        // reset the track errors
        TrackErrors.reset();

        if (Files.exists(dirPath)) {
            Files.walkFileTree(dirPath, new SimpleFileVisitor<Path>() {
                @Override
                public FileVisitResult visitFile(Path file, BasicFileAttributes attrs) throws IOException {
                    Files.delete(file);
                    return FileVisitResult.CONTINUE;
                }

                @Override
                public FileVisitResult postVisitDirectory(Path dir, IOException exc) throws IOException {
                    Files.delete(dir);
                    return FileVisitResult.CONTINUE;
                }
            });
        }
    }

    // Resets the tracksErrors array list
    @AfterEach
    public void resetErrors() {
        TrackErrors.reset();
    }
}
