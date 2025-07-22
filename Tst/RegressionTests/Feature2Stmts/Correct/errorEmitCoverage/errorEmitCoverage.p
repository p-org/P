// Test for emit_coverage primitive focusing on edge cases and error scenarios

machine Main {
    start state Init {
        entry {
            var str : string;
            var map : map[string, int];
            var seq : seq[int];
            
            // Initialize data structures
            map = map[string, int]();
            seq = seq[int]();
            
            // Test with null label (should be handled as empty string)
            str = null;
            emit_coverage str, "Null label test";
            
            // Test with empty label
            emit_coverage "", "Empty label";
            
            // Test with very long label
            str = GenerateVeryLongString();
            emit_coverage str, "Long label test";
            
            // Test with deeply nested data structure
            emit_coverage "NestedData", CreateNestedStructure();
            
            // Test with empty payload
            emit_coverage "EmptyPayload", "";
            
            // Test with special characters in label
            emit_coverage "Special\\Characters\n\t\"Test", "Testing escaping";
            
            // Test with Unicode characters
            emit_coverage "Unicode_☺_😊_⚠️", "Unicode payload_☢️_🔥";
            
            goto BoundaryTests;
        }
    }
    
    state BoundaryTests {
        entry {
            var i : int;
            
            // Test with boundary value integers
            emit_coverage "MaxIntValue", 2147483647;  // Max int32 value
            emit_coverage "MinIntValue", -2147483648; // Min int32 value
            
            // Test with large data structures as payload
            emit_coverage "LargeSeq", CreateLargeSequence();
            emit_coverage "LargeMap", CreateLargeMap();
            
            // Test recursive/cyclical structure handling
            var recData : (int, any);
            recData = (42, null);
            recData = (42, recData);  // Create a self-referential structure
            emit_coverage "RecursiveStructure", recData;
            
            // Test with null payload
            emit_coverage "NullPayload", null;
            
            // Test multiple coverage points in sequence
            i = 0;
            while (i < 10) {
                emit_coverage "RepeatedPoint", i;
                i = i + 1;
            }
        }
    }
    
    fun GenerateVeryLongString() : string {
        var i : int;
        var result : string;
        
        result = "";
        i = 0;
        
        // Generate a 1000+ character string
        while (i < 100) {
            result = format("{0}{1}", result, "This is a very long string used for testing the emit_coverage primitive with extremely long labels. ");
            i = i + 1;
        }
        
        return result;
    }
    
    fun CreateNestedStructure() : (int, (string, (bool, seq[int]))) {
        var inner : seq[int];
        inner = seq[int]();
        
        // Add some elements
        inner += (1);
        inner += (2);
        inner += (3);
        
        return (42, ("nested", (true, inner)));
    }
    
    fun CreateLargeSequence() : seq[int] {
        var result : seq[int];
        var i : int;
        
        result = seq[int]();
        i = 0;
        
        // Create a large sequence (1000 elements)
        while (i < 1000) {
            result += (i);
            i = i + 1;
        }
        
        return result;
    }
    
    fun CreateLargeMap() : map[string, int] {
        var result : map[string, int];
        var i : int;
        
        result = map[string, int]();
        i = 0;
        
        // Create a large map (500 entries)
        while (i < 500) {
            result[format("key_{0}", i)] = i;
            i = i + 1;
        }
        
        return result;
    }
}
