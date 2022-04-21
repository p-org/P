package psymbolic.commandline;

import org.reflections.Reflections;
import psymbolic.runtime.logger.PSymLogger;
import psymbolic.runtime.scheduler.DPORScheduler;
import psymbolic.runtime.scheduler.IterativeBoundedScheduler;
import psymbolic.runtime.statistics.SolverStats;
import psymbolic.valuesummary.solvers.SolverEngine;

import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.ObjectInputStream;
import java.io.ObjectOutputStream;
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
    	SolverEngine.resetEngine(config.getSolverType(), config.getExprLibType());
        SolverStats.setTimeLimit(config.getTimeLimit());
        SolverStats.setMemLimit(config.getMemLimit());
        PSymLogger.ResetAllConfigurations(config.getVerbosity(), config.getProjectName());

        try {
            // load all the files in the passed jar
            String jarPath = PSymbolic.class
                    .getProtectionDomain()
                    .getCodeSource()
                    .getLocation()
                    .toURI()
                    .getPath();

            LoadAllClassesInJar(jarPath);
            Program p;
            IterativeBoundedScheduler scheduler;

            if (config.getReadFromFile() == "") {
                Set<Class<? extends Program>> subTypes = reflections.getSubTypesOf(Program.class);
                Optional<Class<? extends Program>> program = subTypes.stream().findFirst();
                Object instance = program.get().getDeclaredConstructor().newInstance();
                p = (Program) instance;
                scheduler = new IterativeBoundedScheduler(config);
                if (config.isDpor()) scheduler = new DPORScheduler(config);
                EntryPoint.run(p, scheduler, config);
            } else {
                String readFromFile = config.getReadFromFile();
                System.out.println("Reading program state from file " + readFromFile);
                FileInputStream fis = new FileInputStream(readFromFile);
                ObjectInputStream ois = new ObjectInputStream(fis);
                p = (Program) ois.readObject();
                scheduler = (IterativeBoundedScheduler) ois.readObject();
                System.out.println("Successfully read.");
                EntryPoint.resume(p, scheduler, config);
            }

            if (config.isWriteToFile()) {
                String writeFileName = "output/program_state.out";
                System.out.println("Writing program state in file " + writeFileName);
                FileOutputStream fos = new FileOutputStream(writeFileName);
                ObjectOutputStream oos = new ObjectOutputStream(fos);
                oos.writeObject(p);
                oos.writeObject(scheduler);
                System.out.println("Successfully written.");
            }

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
