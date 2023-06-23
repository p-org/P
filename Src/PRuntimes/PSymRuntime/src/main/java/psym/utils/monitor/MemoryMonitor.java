package psym.utils.monitor;

import com.sun.management.GarbageCollectionNotificationInfo;
import lombok.Getter;

import javax.management.Notification;
import javax.management.NotificationEmitter;
import javax.management.NotificationListener;
import java.lang.management.GarbageCollectorMXBean;
import java.lang.management.ManagementFactory;

public class MemoryMonitor {
    private static NotificationListener notificationListener;
    @Getter
    private static double maxMemSpent = 0;               // max memory in megabytes
    @Getter
    private static double memSpent = 0;               // max memory in megabytes

    public static void setup() {
        memSpent = 0;
        maxMemSpent = 0;

        notificationListener = new NotificationListener() {
            @Override
            public void handleNotification(Notification notification, Object handback) {
                if (notification.getType().equals(GarbageCollectionNotificationInfo.GARBAGE_COLLECTION_NOTIFICATION)) {
                    Runtime runtime = Runtime.getRuntime();
                    memSpent = (runtime.totalMemory() - runtime.freeMemory()) / 1000000.0;
                    if (maxMemSpent < memSpent)
                        maxMemSpent = memSpent;
                }
            }
        };

        // register our listener with all gc beans
        for (GarbageCollectorMXBean gcBean : ManagementFactory.getGarbageCollectorMXBeans()) {
            NotificationEmitter emitter = (NotificationEmitter) gcBean;
            emitter.addNotificationListener(notificationListener, null, null);
        }
    }
}
