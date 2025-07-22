// Test for emit_coverage primitive with expression-based labels and payloads

machine Main {
    start state Init {
        entry {
            var counter : int;
            var prefix : string;
            
            counter = 1;
            prefix = "Test";
            
            // Using variable as label
            emit_coverage prefix, "simple payload";
            
            // Using string concatenation for label
            emit_coverage format("{0}_{1}", prefix, counter), counter;
            
            // Using formatted string for label
            emit_coverage format("{0}_point_{1}", prefix, counter), counter * 2;
            
            // Using function call for label
            emit_coverage GetLabel(counter), counter;
            
            goto Next;
        }
    }
    
    state Next {
        entry {
            // Test with dynamic values
            DynamicCoverage("dynamic", 42);
        }
    }
    
    fun GetLabel(id : int) : string {
        return format("function_label_{0}", id);
    }
    
    fun DynamicCoverage(prefix : string, val : int) {
        emit_coverage format("{0}_coverage", prefix), val;
    }
}
