A P program is a collection of state machines communicating with each other by exchanging **events**.
An event in P has two parts: an event name and a payload value (optional) that can be sent along with the event.

??? note "P Event Declaration Grammar"

    ```
    eventDecl :
        | event iden (: type)?;     # P Event Declaration
    ```

    `iden` is the name of the event and `type` is any P data type ([described here](datatypes.md)).

**Syntax:** `event eName;` or `event eName : payloadType;`

`eName` is the name of the P event and `payloadType` is the type of payload values that can be sent along with this event.

=== "Event Declarations"

    ``` java
    // declarations of events with no payloads
    event ePing;
    event ePong;

    // declaration of events that have payloads
    type tRequest = (client: Client, key: string, value: int, requestId: int);
    // eRequest event with payload of type tRequest
    event eRequest: tRequest;
    // eResponse event that can have a payload of type (requestId: int, status: bool)
    event eResponse: (requestId: int, status: bool);
    ```
