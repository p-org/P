package pexplicit.values;

import lombok.Getter;
import pexplicit.values.exceptions.ComparingPValuesException;
import pexplicit.values.exceptions.NamedTupleFieldNameException;

import java.util.HashMap;
import java.util.List;
import java.util.Map;

/**
 * Represents the PValue for P named tuple
 */
public class PNamedTuple extends PValue<PNamedTuple> {
    @Getter
    private final Map<String, PValue<?>> values;

    /**
     * Constructor
     *
     * @param input_fields List of field names
     * @param input_values List of values corresponding to each field
     */
    public PNamedTuple(List<String> input_fields, List<PValue<?>> input_values) {
        assert (input_fields.size() == input_values.size());
        values = new HashMap<>();
        for (int i = 0; i < input_fields.size(); i++) {
            values.put(input_fields.get(i), input_values.get(i));
        }
        initialize();
    }

    /**
     * Constructor
     *
     * @param input_values Map from field name to corresponding field value
     */
    public PNamedTuple(Map<String, PValue<?>> input_values) {
        values = new HashMap<>();
        for (Map.Entry<String, PValue<?>> entry : input_values.entrySet()) {
            values.put(entry.getKey(), PValue.clone(entry.getValue()));
        }
        initialize();
    }

    /**
     * Copy constructor
     *
     * @param other value to copy from
     */
    public PNamedTuple(PNamedTuple other) {
        this(other.values);
    }

    /**
     * Get the value corresponding to a field name.
     *
     * @param name field name
     * @return value corresponding to the field name
     * @throws NamedTupleFieldNameException
     */
    public PValue<?> getField(String name) throws NamedTupleFieldNameException {
        if (!values.containsKey(name)) throw new NamedTupleFieldNameException(this, name);
        return values.get(name);
    }

    /**
     * Get the value corresponding to a field name given as a PString.
     *
     * @param name field name
     * @return value corresponding to the field name
     * @throws NamedTupleFieldNameException
     */
    public PValue<?> getField(PString name) throws NamedTupleFieldNameException {
        return getField(name.toString());
    }

    /**
     * Set the value corresponding to a field name.
     * Note that the field name must be present in the fields list.
     * Otherwise, a NamedTupleFieldNameException is thrown.
     *
     * @param name field name
     * @param val  value to set
     * @throws NamedTupleFieldNameException
     */
    public PNamedTuple setField(String name, PValue<?> val) throws NamedTupleFieldNameException {
        if (!values.containsKey(name)) throw new NamedTupleFieldNameException(this, name);
        Map<String, PValue<?>> newValues = new HashMap<>(values);
        newValues.put(name, val);
        return new PNamedTuple(newValues);
    }

    /**
     * Set the value corresponding to a field name given as a PString.
     * Note that the field name must be present in the fields list.
     * Otherwise, a NamedTupleFieldNameException is thrown.
     *
     * @param name field name
     * @param val  value to set
     * @throws NamedTupleFieldNameException
     */
    public PNamedTuple setField(PString name, PValue<?> val) throws NamedTupleFieldNameException {
        return setField(name.toString(), val);
    }

    @Override
    public PNamedTuple clone() {
        return new PNamedTuple(values);
    }

    @Override
    protected String _asString() {
        StringBuilder sb = new StringBuilder();
        sb.append("(");
        boolean hadElements = false;
        for (String name : values.keySet()) {
            if (hadElements) {
                sb.append(", ");
            }
            sb.append(name);
            sb.append(": ");
            sb.append(values.get(name));
            hadElements = true;
        }
        sb.append(")");
        return sb.toString();
    }

    @Override
    public PNamedTuple getDefault() {
        Map<String, PValue<?>> defaultValues = new HashMap<>();
        for (Map.Entry<String, PValue<?>> entry : values.entrySet()) {
            PValue<?> val = entry.getValue();
            if (val == null) {
                defaultValues.put(entry.getKey(), null);
            } else {
                defaultValues.put(entry.getKey(), val.getDefault());
            }
        }
        return new PNamedTuple(defaultValues);
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) return true;

        if (!(obj instanceof PNamedTuple other)) {
            return false;
        }

        if (values.size() != other.values.size()) {
            return false;
        }

        for (String name : values.keySet()) {
            if (!other.values.containsKey(name)) {
                throw new ComparingPValuesException(other, this);
            } else if (PValue.notEqual(other.values.get(name), this.values.get(name))) {
                return false;
            }
        }
        return true;
    }
}
