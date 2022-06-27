package psymbolic.runtime.concretevalues;
import psymbolic.runtime.concretevalues.exceptions.*;

import java.util.Map;
import java.util.HashMap;

public class PNamedTuple extends PValue<PNamedTuple> {
    // stores the mapping from field name to field value
    private final Map<String, PValue<?>> fields;

    public PNamedTuple(Map<String, PValue<?>> input_fields)
    {
        fields = new HashMap<>();
        for (Map.Entry<String, PValue<?>> entry : input_fields.entrySet()) {
            fields.put(entry.getKey(), PValue.clone(entry.getValue()));
        }
    }

    public PNamedTuple(PNamedTuple other)
    {
        fields = new HashMap<>();
        for (Map.Entry<String, PValue<?>> entry : other.fields.entrySet()) {
            fields.put(entry.getKey(), PValue.clone(entry.getValue()));
        }
    }

    public String[] getFields() {
        return fields.keySet().toArray(new String[0]);
    }

    public PValue<?> getField(String name) throws NamedTupleFieldNameException {
        if(!fields.containsKey(name))
            throw new NamedTupleFieldNameException(this, name);
        return fields.get(name);
    }

    public void setField(String name, PValue<?> val) throws NamedTupleFieldNameException {
        if(!fields.containsKey(name))
            throw new NamedTupleFieldNameException(this, name);
        fields.put(name, val);
    }

    public PValue<?> getField(PString name) throws NamedTupleFieldNameException {
        return getField(name.toString());
    }

    public void setField(PString name, PValue<?> val) throws NamedTupleFieldNameException {
        setField(name.toString(), val);
    }

    @Override
    public PNamedTuple clone() {
        return new PNamedTuple(fields);
    }

    @Override
    public int hashCode() {
        return ComputeHash.getHashCode(fields.values())
                ^ ComputeHash.getHashCode(fields.keySet());
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this)
            return true;

        if (!(obj instanceof PNamedTuple)) {
            return false;
        }

        PNamedTuple other = (PNamedTuple) obj;
        if (fields.size() != other.fields.size()) {
            return false;
        }

        for (String name : fields.keySet()) {
            if (!other.fields.containsKey(name)) {
                throw new ComparingPValuesException(other, this);
            } else if (!PValue.equals(other.fields.get(name), this.fields.get(name))) {
                return false;
            }
        }
        return true;
    }

    @Override
    public String toString() {
        StringBuilder sb = new StringBuilder();
        sb.append("(");
        boolean hadElements = false;
        for (String name : fields.keySet()) {
            if (hadElements) {
                sb.append(", ");
            }
            sb.append(name);
            sb.append(": ");
            sb.append(fields.get(name));
            hadElements = true;
        }
        sb.append(")");
        return sb.toString();
    }
}
