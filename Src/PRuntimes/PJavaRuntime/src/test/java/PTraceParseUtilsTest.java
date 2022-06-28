import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

import parsers.*;

import java.util.List;

import static org.junit.jupiter.api.Assertions.assertEquals;

public class PTraceParseUtilsTest {
    @Test
    @DisplayName("Named tuple conversion to kv pairs works")
    public void testNamedTupleToKVPairs() {
        String token = "<status:0, accountId:0, balance:10, rId:7, >";
        List<String> kvs = PTraceParserUtils.Conversions.namedTupleToKVPairs(token);
        assertEquals(4, kvs.size());
        assertEquals("status:0", kvs.get(0));
        assertEquals("accountId:0", kvs.get(1));
        assertEquals("balance:10", kvs.get(2));
        assertEquals("rId:7", kvs.get(3));
    }

    @Test
    @DisplayName("Named tuple conversion with string literals and escapes works")
    public void testNamedTupleWithStringLitsToKVPairs() {
        String token = "<s:\"a:string, with:\\\"problems\", >";
        List<String> kvs = PTraceParserUtils.Conversions.namedTupleToKVPairs(token);
        assertEquals(1, kvs.size());
        assertEquals("s:\"a:string, with:\\\"problems\"", kvs.get(0));
    }

    @Test
    @DisplayName("Tuple conversion to elements works")
    public void testTupleToKVPairs() {
        String token = "<42,99,>";
        List<String> vals = PTraceParserUtils.Conversions.tupleToValues(token);
        assertEquals(2, vals.size());
        assertEquals("42", vals.get(0));
        assertEquals("99", vals.get(1));
    }

    @Test
    @DisplayName("KV pair conversion to primitive types works")
    public void testKVPairToPrimConversion() {
        assertEquals(42, PTraceParserUtils.Conversions.kvPairToInt("key:42"));
        assertEquals(42L, PTraceParserUtils.Conversions.kvPairToLong("key:42"));
        assertEquals(3.14, PTraceParserUtils.Conversions.kvPairToFloat("key:3.14"), 0.0001);

        assertEquals("hello", PTraceParserUtils.Conversions.kvPairToString("key:hello"));

        //Technically this could be either "world" or "hello:world", but since P identifiers can't have colons
        //in them it's far more likely that the value string has a colon in it.
        assertEquals("hello:world", PTraceParserUtils.Conversions.kvPairToString("key:hello:world"));
    }

}
