package psymbolic.commandline;

import org.reflections.Reflections;
import psymbolic.runtime.logger.PSymLogger;

import java.io.FileInputStream;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;
import java.util.Set;
import java.util.jar.JarEntry;
import java.util.jar.JarInputStream;

public class PSymbolic {

    public static void main(String[] args) {
        // parse the commandline arguments to create the configuration
        PSymConfiguration config = PSymOptions.ParseCommandlineArgs(args);
        Reflections reflections = new Reflections("psymbolic");

        try {
            // load all the files in the passed jar
            String jarPath = PSymbolic.class
                    .getProtectionDomain()
                    .getCodeSource()
                    .getLocation()
                    .toURI()
                    .getPath();

            LoadAllClassesInJar(jarPath);

            Set<Class<? extends Program>> subTypes = reflections.getSubTypesOf(Program.class);
            Optional<Class<? extends Program>> program = subTypes.stream().findFirst();
            Object instance = program.get().getDeclaredConstructor().newInstance();
            EntryPoint.run((Program) instance, config);
        } catch (BugFoundException e) {
            e.printStackTrace();
            System.exit(2);
        }
        catch (Exception ex)
        {
            ex.printStackTrace();
            System.exit(10);
        }
    }

    /**
     * Loads all the classes in the specified Jar
     * @param pathToJar concrete path to the Jar
     * @return all the classes in the specified Jar
     */
    public static List<Class> LoadAllClassesInJar(String pathToJar) {
        List<Class> classes = new ArrayList<>();
        try {

            final JarInputStream jarFile = new JarInputStream(
                    new FileInputStream(pathToJar));
            JarEntry jarEntry;

            PSymLogger.info("Loading:: " + pathToJar);
            while (true) {
                jarEntry = jarFile.getNextJarEntry();
                if (jarEntry == null) {
                    break;
                }
                final String classPath = jarEntry.getName();
                if (classPath.startsWith("psymbolic") && classPath.endsWith(".class")) {
                    final String className = classPath
                            .substring(0, classPath.length() - 6).replace('/', '.');

                    //System.out.println("Found entry " + jarEntry.getName());

                    try {
                        classes.add(Class.forName(className));
                    } catch (final ClassNotFoundException x) {
                        PSymLogger.error("Cannot load class " + className + " " + x);
                    }
                }
            }
            jarFile.close();

        } catch (final Exception e) {
            e.printStackTrace();
        }

        return classes;
    }

}
