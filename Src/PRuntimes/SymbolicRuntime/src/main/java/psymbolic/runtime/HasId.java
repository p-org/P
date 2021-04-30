package psymbolic.runtime;

public abstract class HasId {
    public final String name;
    public final long id;

    public HasId(String name, long id) {
        this.name = name;
        this.id = id;
    }

    @Override
    public boolean equals(Object obj) {
        if (obj.getClass().equals(this.getClass())) {
            HasId m = (HasId) obj;
            return this.name.equals(m.name) && this.id == m.id;
        }
        return false;
    }

    @Override
    public int hashCode() {
        return (name + id).hashCode();
    }
}
