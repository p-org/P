// Test for emit_coverage primitive with payloads of different types

machine Main {
    start state Init {
        entry {
            var intVal : int;
            var boolVal : bool;
            var strVal : string;
            
            intVal = 42;
            boolVal = true;
            strVal = "test string";
            
            // Basic usage with string literal and various payload types
            emit_coverage "WithIntPayload", intVal;
            emit_coverage "WithBoolPayload", boolVal;
            emit_coverage "WithStringPayload", strVal;
            
            // Use null payload
            emit_coverage "WithNullPayload", null;
            
            // Use expressions as payload
            emit_coverage "WithIntExpressionPayload", intVal * 2;
            emit_coverage "WithBoolExpressionPayload", intVal > 20;
            
            // Call function with payload tests
            TestNumericPayloads();
            
            // Continue test
            goto Next;
        }
    }
    
    state Next {
        entry {
            var tuple : (int, string);
            tuple = (123, "tuple value");
            
            // Test with tuple payload
            emit_coverage "WithTuplePayload", tuple;
            
            // Test with complex payload from function call
            emit_coverage "WithFunctionResultPayload", GetPayload();
        }
    }
    
    fun TestNumericPayloads() {
        // Test emit_coverage with numeric payloads in function
        emit_coverage "NegativeIntPayload", -100;
        emit_coverage "ZeroPayload", 0;
        emit_coverage "FloatPayload", 3.14;
    }
    
    fun GetPayload() : (int, bool, string) {
        return (42, false, "complex payload");
    }
}
