## Importance of Liveness Specifications

When reasoning about the correctness of a distributed system, **it is really important to specify both safety as well as liveness specifications**.

!!! note ""
    The examples in [Tutorials](../tutsoutline.md) show how to specify both safety and liveness specifications using [P Monitors](../manual/monitors.md).

!!! warning "Always specify both safety and liveness specifications"
    Only specifying safety properties is not enough — a system model may be incorrect and in the worst case drop all requests without performing any operation. Such a system trivially satisfies all safety specifications! Hence, it is essential to combine safety with **liveness properties** to check that the system is making progress and servicing requests.

    Running the checker on models that have both safety and liveness properties ensures that for all executions explored by the checker, requests are eventually serviced by the system and all responses satisfy the desired correctness specification. This prevents models from doing something trivially incorrect like always doing nothing :smile:, in which case running the checker adds no value.

---

### Example: Client Server

In the [client server example](../tutorial/clientserver.md):

| Specification | Type | What it checks |
|--------------|------|----------------|
| [BankBalanceIsAlwaysCorrect](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PSpec/BankBalanceCorrect.p#L4) | **Safety** | The response sent by the bank server is always correct |
| [GuaranteedWithDrawProgress](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PSpec/BankBalanceCorrect.p#L91) | **Liveness** | The system will always eventually send a response |

Combining both ensures that the system **will eventually respond** (liveness) and that **every response is correct** (safety).
