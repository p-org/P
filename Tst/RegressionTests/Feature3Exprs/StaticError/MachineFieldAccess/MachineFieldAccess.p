// Test case for GitHub issue #920
// Accessing a field of a machine reference should produce a compile error
// rather than an unhandled ArgumentOutOfRangeException

event eStart;

machine Server {
    var database: int;

    start state Init {
        entry {
            database = 42;
        }
    }
}

machine Client {
    var server: Server;
    var x: int;

    start state Init {
        entry (s: Server) {
            server = s;
            // This should produce a static error - cannot access machine fields
            x = server.database;
        }
    }
}
