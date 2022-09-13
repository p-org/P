package psymbolic.commandline;

import org.reflections.Reflections;
import psymbolic.runtime.logger.BacktrackLogger;
import psymbolic.runtime.logger.PSymLogger;
import psymbolic.runtime.logger.StatLogger;
import psymbolic.runtime.scheduler.DPORScheduler;
import psymbolic.runtime.scheduler.IterativeBoundedScheduler;
import psymbolic.runtime.statistics.SolverStats;
import psymbolic.utils.MemoryMonitor;
import psymbolic.utils.RandomNumberGenerator;
import psymbolic.utils.TimeMonitor;
import psymbolic.valuesummary.solvers.SolverEngine;

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
        PSymLogger.ResetAllConfigurations(config.getVerbosity(), config.getProjectName(), config.getOutputFolder());
        Reflections reflections = new Reflections("psymbolic");
    	SolverEngine.resetEngine(config.getSolverType(), config.getExprLibType());
        SolverStats.setTimeLimit(config.getTimeLimit());
        SolverStats.setMemLimit(config.getMemLimit());
        MemoryMonitor.setup();
        RandomNumberGenerator.setup(config.getRandomSeed());
        TimeMonitor.setup(config.getTimeLimit());

        int exit_code = 0;
        try {
            // load all the files in the passed jar
            String jarPath = PSymbolic.class
                    .getProtectionDomain()
                    .getCodeSource()
                    .getLocation()
                    .toURI()
                    .getPath();

            LoadAllClassesInJar(jarPath);
            IterativeBoundedScheduler scheduler;

            if (config.getReadFromFile() == "") {
                Set<Class<? extends Program>> subTypesProgram = reflections.getSubTypesOf(Program.class);
                if(subTypesProgram.stream().count() == 0) {
                    throw new Exception("No program found.");
                }

                Optional<Class<? extends Program>> program = subTypesProgram.stream().findFirst();
                Object instance = program.get().getDeclaredConstructor().newInstance();
                Program p = (Program) instance;
                setTestDriver(p, config);
                scheduler = new IterativeBoundedScheduler(config, p);
                if (config.isDpor()) scheduler = new DPORScheduler(config, p);
                EntryPoint.run(scheduler, config);
            } else {
                scheduler = IterativeBoundedScheduler.readFromFile(config.getReadFromFile());
                EntryPoint.resume(scheduler, config);
            }

            if (config.isWriteToFile()) {
                BacktrackLogger.Initialize(config.getProjectName(), config.getOutputFolder());
                EntryPoint.writeToFile();
            }

        } catch (BugFoundException e) {
            e.printStackTrace();
            exit_code = 2;
        }
        catch (Exception ex) {
            ex.printStackTrace();
            exit_code = 10;
        } finally {
            if (config.getCollectStats() != 0) {
                double postSearchTime = TimeMonitor.getInstance().stopInterval();
                StatLogger.log("time-post-seconds", String.format("%.1f", postSearchTime), false);
            }
            System.exit(exit_code);
        }
    }

    /**
     * Set the test driver for the program
     * @param p Input program instance
     * @param config Input PSymConfiguration
     * @throws Exception Throws exception if test driver is not found
     */
    public static void setTestDriver(Program p,PSymConfiguration config) throws Exception {
        String name = config.getTestDriver();
        Reflections reflections = new Reflections("psymbolic");
        Set<Class<? extends PTestDriver>> subTypesDriver = reflections.getSubTypesOf(PTestDriver.class);
        PTestDriver driver = null;
        for (Class<? extends PTestDriver> td: subTypesDriver) {
            if (td.getSimpleName().equalsIgnoreCase(name)) {
                driver = td.getDeclaredConstructor().newInstance();
                PSymLogger.info("Setting Test Driver:: " + td.getSimpleName());
                break;
            }
        }
        if(driver == null
           && name.equals(config.getTestDriverDefault())
           && subTypesDriver.size() == 1) {
            for (Class<? extends PTestDriver> td: subTypesDriver) {
                driver = td.getDeclaredConstructor().newInstance();
                PSymLogger.info("Setting Test Driver to Default:: " + td.getSimpleName());
                break;
            }
        }
        if(driver == null) {
            PSymLogger.info("No test driver found named \"" + name + "\"");
            PSymLogger.info("Possible Test Drivers::");
            for (Class<? extends PTestDriver> td: subTypesDriver) {
                PSymLogger.info("\t" + td.getSimpleName());
            }
            throw new Exception("No test driver found named \"" + name + "\"");
        }
        p.setTestDriver(driver);
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
