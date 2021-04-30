package symbolicp;

import symbolicp.runtime.Machine;

import java.lang.reflect.Field;
import java.util.ArrayList;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.stream.StreamSupport;

public class Utils {

    public static Machine[] getMachines(Class<?> wrapper_class, Object wrapper) throws IllegalAccessException {
        ArrayList<Machine> machines = new ArrayList<>();
        // Collect all machine tag fields from the class
        for (Field field : wrapper_class.getFields()) {
            if (field.getType().isInstance(Machine.class)) {
                machines.add((Machine) field.get(wrapper));
            }
        }
        Machine[] machinesArr = new Machine[machines.size()];
        machines.toArray(machinesArr);
        return machinesArr;
    }

    public static String[] splitPath(String pathString) {
        Path path = Paths.get(pathString);
        return StreamSupport.stream(path.spliterator(), false).map(Path::toString)
                .toArray(String[]::new);
    }
}
