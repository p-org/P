package psymbolic.runtime;

public interface EventName {

    class Init implements EventName{
        public static final Init instance = new Init();
        @Override
        public String toString() {
            return "Init";
        }
    }

}
