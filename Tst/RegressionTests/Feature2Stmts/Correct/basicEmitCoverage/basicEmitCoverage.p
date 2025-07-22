// Basic test for emit_coverage primitive with simple string literals as labels

machine Main {
    start state Init {
        entry {
            // Basic usage with string literals
            emit_coverage "InitEntry";
            
            // Call a function that uses emit_coverage
            foo();
            
            // Multiple emit_coverage statements in sequence
            emit_coverage "BeforeGoto";
            
            // Transition to another state
            goto Next;
        }
    }
    
    state Next {
        entry {
            // Test emit_coverage in a different state
            emit_coverage "NextEntry";
            
            // Emit coverage with different types of strings
            emit_coverage "With spaces and symbols: !@#$";
            emit_coverage "";  // Empty string
            
            // Done with test
            emit_coverage "Done";
        }
    }
    
    fun foo() {
        // Test emit_coverage inside a function
        emit_coverage "InsideFunction";
    }
}
