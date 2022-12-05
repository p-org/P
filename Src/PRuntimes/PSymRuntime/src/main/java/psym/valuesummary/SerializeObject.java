package psym.valuesummary;

import java.io.*;
import java.util.Base64;

public class SerializeObject {

    public static Object objectFromString(String s) {
        try {
            byte [] data = Base64.getDecoder().decode(s);
            ObjectInputStream ois = null;
            ois = new ObjectInputStream(new ByteArrayInputStream(data));
            Object o  = ois.readObject();
            ois.close();
            return o;
        } catch (IOException e) {
            e.printStackTrace();
        } catch (ClassNotFoundException e) {
            e.printStackTrace();
        }
        return null;
    }

    public static String serializableToString(Serializable o) {
        try {
            ByteArrayOutputStream baos = new ByteArrayOutputStream();
            ObjectOutputStream oos = null;
            oos = new ObjectOutputStream(baos);
            oos.writeObject(o);
            oos.close();
            return Base64.getEncoder().encodeToString(baos.toByteArray());
        } catch (IOException e) {
            e.printStackTrace();
        }
        return null;
    }
}
