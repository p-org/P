import java.util.Random;
import p.runtime.values.*;

public class GlobalFunctions {

    public static Random rand = new Random();
    public static int ctr = 0;

    public static IntHolder func_getIntHolder() {
       return new IntHolder();
    }

    public static PInt func_randomInt(IntHolder holder) {
       System.out.println("Int held: " + holder.heldInt);
       int i = rand.nextInt(holder.heldInt + 1);
       System.out.println("Int chosen: " + ctr);
       return new PInt(ctr++);
    }

}
