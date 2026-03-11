// Test case for GitHub issue #920 - machine field access in expressions
// Using machine field access in arithmetic expressions should produce a compile error

event eStart;

machine Counter {
    var count: int;

    start state Init {
        entry {
            count = 10;
        }
    }
}

machine Client {
    var counter: Counter;
    var result: int;

    start state Init {
        entry (c: Counter) {
            counter = c;
            // This should produce a static error - machine field access in expression
            result = counter.count + 5;
        }
    }
}
