package psymbolic.utils;

import java.util.Random;

public class RandomNumberGenerator {
    private static RandomNumberGenerator randomNumberGenerator;
    private Random rand;

    private RandomNumberGenerator(int seed) {
        rand = new Random(seed);
    }

    public static void setup(int seed) {
        randomNumberGenerator = new RandomNumberGenerator(seed);
    }

    public static RandomNumberGenerator getInstance() {
        assert(randomNumberGenerator != null);
        return randomNumberGenerator;
    }
    public long getRandomLong() {
        return rand.nextLong();
    }

}
