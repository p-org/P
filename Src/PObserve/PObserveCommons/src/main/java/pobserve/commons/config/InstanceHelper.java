package pobserve.commons.config;

public interface InstanceHelper {

    boolean isInstanceOf(Class c, String className);

    Object getInstance(Class c);

    static InstanceHelper getHelper(InstanceType type) {
        switch (type) {
            case PARSER:
                return new ParserHelper();
            case SPECIFICATION_SUPPLIER:
                return new SpecSupplierHelper();
            case FILTER:
                return new FilterHelper();
            default:
                throw new RuntimeException("No implementation for supplier helper of type " + type);
        }
    }
}
