package pobserve.commons.config;

import java.io.IOException;
import java.net.MalformedURLException;
import java.net.URL;
import java.net.URLClassLoader;
import java.util.ArrayList;
import java.util.Enumeration;
import java.util.List;
import java.util.jar.JarEntry;
import java.util.jar.JarFile;
import java.util.stream.Collectors;

import pobserve.commons.exceptions.PObserveInternalException;
import com.beust.jcommander.ParameterException;

/*
* Jarloader class is used to load classes in the input jar and
*  create parser, filter and specification supplier instances
* */
public final class CreateInstanceFromJar {
    private final ClassLoader classLoader;
    private final List<Class> allClasses;

    public CreateInstanceFromJar(List<String> jarFilePaths, ClassLoader pobserveClassLoader) {
        classLoader = getClassLoader(jarFilePaths, pobserveClassLoader);
        allClasses = getAllClasses(jarFilePaths);
    }

    /*
     * Returns a new instance of requested type from the input jar
     * @param type can be parser, specification or filter
     * @param className is the exact classname of the instance needed
     * @return a new instance of requested type
     * */
    public Object getInstance(InstanceType type, String className) {
        InstanceHelper helper = InstanceHelper.getHelper(type);
        List<Class> filteredClasses = allClasses.stream().filter(c -> helper.isInstanceOf(c, className))
                .collect(Collectors.toList());

        if (filteredClasses.isEmpty()) {
            throw new ParameterException(type + " not found in the specified jar.");
        } else if (filteredClasses.size() == 1) {
            try {
                return helper.getInstance(filteredClasses.get(0));
            } catch (Exception e) {
                throw new PObserveInternalException("Exception occurred while creating " + type + " instance", e);
            }
        } else {
            String message = "Found multiple " + type + "s in the jar("
                    + filteredClasses.stream().map(Class::getSimpleName).collect(Collectors.joining(", "))
                    + ")";
            throw new ParameterException(message);
        }
    }

    /*
     * Returns class loader to load input supplierJars
     * @param jarFilePaths list of supplierJars to be loaded
     * @param cl PObserve class loader
     * @return PObserve class loader that load all classes from the input jar
     */
    private static ClassLoader getClassLoader(List<String> jarFilePaths, ClassLoader cl) {
        URL[] urls = jarFilePaths.stream().map(
                                jarFilePath -> {
                                    try {
                                        return new URL("file:" + jarFilePath);
                                    } catch (MalformedURLException e) {
                                        throw new RuntimeException(e);
                                    }
                                })
                        .toArray(URL[]::new);
        return URLClassLoader.newInstance(urls, cl);
    }

    /*
     * Returns a list of all classes from input supplierJars
     * @param jarPaths list of input jar paths
     * @return list of all the classes in the supplierJars
     */
    private List<Class> getAllClasses(List<String> jarPaths) {
        List<Class> allClasses = new ArrayList<>();

        for (String pathToJar: jarPaths) {
            allClasses.addAll(getAllClassesInJar(pathToJar, classLoader));
        }

        return allClasses;
    }

    /*
     * Returns all classes that can be loaded in an input jar
     * @param pathToJar path of the jar that needs to be loaded
     * @param cl PObserve class loader
     * @return list of all classes in the jar loaded using PObserve class
     */
    private static List<Class> getAllClassesInJar(String pathToJar, ClassLoader cl) {
        List<Class> classes = new ArrayList<>();
        JarFile jarFile = null;
        try {
            jarFile = new JarFile(pathToJar);
        } catch (IOException e) {
            throw new PObserveInternalException("Exception when loading supplied input jar", e);
        }
        for (Enumeration<JarEntry> e = jarFile.entries(); e.hasMoreElements();) {
            try {
                JarEntry je = e.nextElement();
                String className = je.getName().replace('/', '.').replace(".class", "");
                classes.add(cl.loadClass(className));
            } catch (Error | Exception ignored) {
                continue;
            }
        }
        try {
            jarFile.close();
        } catch (IOException ex) {
            throw new PObserveInternalException("Exception when loading classes in the supplied input jar", ex);
        }
        return classes;
    }
}
