package pcontainment.runtime;

import com.microsoft.z3.Expr;

import java.util.HashMap;
import java.util.Map;
import java.util.Set;

public class Payloads {
    private final Map<String, Object> payloads;

    public Payloads() {
        this.payloads = new HashMap<>();
    }

    public Payloads(Map<String, Object> payloads) {
        this.payloads = new HashMap<>(payloads);
    }

    public Payloads(Object ... payloads) {
        this.payloads = new HashMap<>();
        for (int i = 0; i < payloads.length; i += 2) {
            this.payloads.put((String) payloads[i], payloads[i + 1]);
        }
    }

    public void put(String key, Object value) {
        this.payloads.put(key, value);
    }

    public Object get(String key) {
        return this.payloads.get(key);
    }

    public Set<Map.Entry<String, Object>> entrySet() {
        return payloads.entrySet();
    }

    public boolean containsField(String name) {
        return payloads.containsKey(name);
    }
}
