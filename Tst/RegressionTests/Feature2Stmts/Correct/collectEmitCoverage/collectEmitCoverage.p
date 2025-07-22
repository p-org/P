// Test for emit_coverage to verify proper collection of coverage data
// This test has multiple execution paths with coverage points in each

// Define events for testing different execution paths
event eStart;
event ePathA;
event ePathB; 
event ePathC;
event eFinish;

machine Main {
    var choice : int;
    
    start state Init {
        entry {
            // Base coverage point
            emit_coverage "InitEntry", "Coverage collection started";
            
            // Create test machine
            new CoverageTestMachine(this);
            
            // Raise start event
            raise eStart;
        }
        
        on eStart do {
            // Choose a random path for testing
            choice = 1 + (nondet(2));  // 1, 2, or 3
            emit_coverage "PathSelection", choice;
            
            // Follow different paths to test coverage collection
            if (choice == 1) {
                raise ePathA;
            } else if (choice == 2) {
                raise ePathB;
            } else {
                raise ePathC;
            }
        }
        
        on ePathA goto PathA;
        on ePathB goto PathB;
        on ePathC goto PathC;
    }
    
    state PathA {
        entry {
            // Path A coverage point
            emit_coverage "PathA_Entry", "Executed path A";
            
            // Execute path-specific code with its own coverage
            DoPathAWork();
            
            // Signal completion
            raise eFinish;
        }
        
        on eFinish goto Final;
    }
    
    state PathB {
        entry {
            // Path B coverage point
            emit_coverage "PathB_Entry", "Executed path B";
            
            // Execute path-specific code with its own coverage
            DoPathBWork();
            
            // Signal completion
            raise eFinish;
        }
        
        on eFinish goto Final;
    }
    
    state PathC {
        entry {
            // Path C coverage point
            emit_coverage "PathC_Entry", "Executed path C";
            
            // Execute path-specific code with its own coverage
            DoPathCWork();
            
            // Signal completion
            raise eFinish;
        }
        
        on eFinish goto Final;
    }
    
    state Final {
        entry {
            // Final coverage point
            emit_coverage "FinalEntry", "Test finished";
            
            // Report selected path
            emit_coverage "SelectedPath", choice;
        }
    }
    
    fun DoPathAWork() {
        // Path-specific coverage points
        emit_coverage "PathA_Work", "Path A work";
        
        // Conditional coverage
        if (choice == 1) {
            emit_coverage "PathA_ChoiceVerified", "Choice matches expected value";
        }
    }
    
    fun DoPathBWork() {
        // Path-specific coverage points
        emit_coverage "PathB_Work", "Path B work";
        
        // Conditional coverage
        if (choice == 2) {
            emit_coverage "PathB_ChoiceVerified", "Choice matches expected value";
        }
    }
    
    fun DoPathCWork() {
        // Path-specific coverage points
        emit_coverage "PathC_Work", "Path C work";
        
        // Conditional coverage
        if (choice == 3) {
            emit_coverage "PathC_ChoiceVerified", "Choice matches expected value";
        }
    }
}

// Secondary machine to verify coverage across multiple machines
machine CoverageTestMachine {
    var parent : Main;
    
    start state Init {
        entry (parentMachine: Main) {
            parent = parentMachine;
            
            // Coverage in secondary machine
            emit_coverage "SecondaryMachine_Init", "Second machine initialized";
        }
    }
}
