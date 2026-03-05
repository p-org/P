## Common: Timer, Failure Injector, and Shared Memory

The tutorials use several reusable P models for common system components. These are located in [Tutorial/Common](https://github.com/p-org/P/tree/master/Tutorial/Common).

---

### :material-timer-outline:{ .lg } Timer

The [Timer](https://github.com/p-org/P/blob/master/Tutorial/Common/Timer/) model captures a system's interaction with an OS timer. It is used in the Two Phase Commit, Espresso Machine, and Failure Detector examples.

---

### :material-alert-circle-outline:{ .lg } Failure Injector

The [Failure Injector](https://github.com/p-org/P/tree/master/Tutorial/Common/FailureInjector) model allows injecting node failures into the system during model checking. It is used in the Two Phase Commit and Failure Detector examples.

---

### :material-memory:{ .lg } Shared Memory

P is a purely message-passing based programming language and does not support primitives for modeling shared memory concurrency. However, shared memory concurrency can always be modeled using message passing.

The [Shared Memory](https://github.com/p-org/P/tree/master/Tutorial/Common/SharedMemory) example demonstrates this approach, which has been used when checking the correctness of single-node file systems.
