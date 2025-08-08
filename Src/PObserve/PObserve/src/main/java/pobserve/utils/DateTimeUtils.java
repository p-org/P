package pobserve.utils;

import java.text.SimpleDateFormat;
import java.util.Date;

public class DateTimeUtils {
    public static String getCurrentDateTime() {
        SimpleDateFormat formatter = new SimpleDateFormat("dd-MM-yyyy_HH:mm:ss");
        Date date = new Date();
        return formatter.format(date);
    }
}
