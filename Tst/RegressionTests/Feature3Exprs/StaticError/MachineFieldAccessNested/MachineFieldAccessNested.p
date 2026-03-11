// Test case for GitHub issue #920 - nested machine field access
// Accessing fields through a chain of machine references should produce a compile error

event eInit: Server;

machine Database {
    var storedValue: int;

    start state Init {
        entry {
            storedValue = 100;
        }
    }
}

machine Server {
    var database: Database;

    start state Init {
        entry {
            database = new Database();
        }
    }
}

machine Client {
    var server: Server;
    var x: int;

    start state Init {
        entry (s: Server) {
            server = s;
            // This should produce a static error - nested machine field access
            x = server.database.storedValue;
        }
    }
}
