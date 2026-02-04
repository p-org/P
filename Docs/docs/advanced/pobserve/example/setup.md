# Tutorial - Enabling a Lock Server Application to Use PObserve

### Intro

Welcome to the Lock Server tutorial. In this tutorial we will walk you through how to set up and use PObserve on a Java Gradle application.

Let’s say you already have a lock server application and you want to make sure that the lock server’s implementation doesn’t violate some crucial correctness properties, what would be the next steps in ensuring the robustness of the system? 

### Application - Lock Server Implemented in Java

Assume your lock server application contains a Lock class and a LockServer class.

* Lock performs lock and release actions on itself
* LockServer keeps track of all locks; it acts as a bridge between clients and locks

Aside from the application itself, assume you also have a log builder class with log4j to record information you’ll need to check against the specifications later.

???note "Check your log4j2.xml file for the configuration for log4j, make sure it logs time that show nanoseconds."

    We recommend logging precise timestamps (with nano second granularity) to properly order events across distributed systems. Here's an example log4j2.xml configuration that includes nanosecond precision:

    ```xml
    <?xml version="1.0" encoding="UTF-8"?>
    <Configuration>
        <Appenders>
            <File name="LogFile" fileName="all.log" immediateFlush="true" append="false" >
                <PatternLayout pattern="%d{yyyy-MM-dd HH:mm:ss.SSS}.%nano [%t] %-5level %logger{1} - %marker: %msg%n"/>
            </File>
        </Appenders>
        <Loggers>
            <Root level="all">
                <AppenderRef ref="LogFile" level="INFO"/>
            </Root>
        </Loggers>
    </Configuration>
    ```

    **Key Configuration Points:**
    
    - **`%d{yyyy-MM-dd HH:mm:ss.SSS}.%nano`**: This pattern includes both milliseconds (`.SSS`) and nanoseconds (`.%nano`) for maximum timestamp precision
    - **`immediateFlush="true"`**: Ensures logs are written immediately, preventing buffering issues that could affect timestamp accuracy
    
    **How to Verify Nanosecond Logging:**
    
    1. Look for the **`.%nano`** field in your PatternLayout pattern
    2. Check your log output - timestamps should show format like: `2023-12-01 16:10:34.000921` where the last 6 digits after the dot represent nanoseconds
    3. Verify that consecutive log entries have different nanosecond values when events occur rapidly
    
    :warning: **Important**: Without nanosecond precision, PObserve may not be able to correctly order events that occur within the same millisecond, potentially leading to incorrect specification checking results.


!!!tip ""

    Writing a good logger that is informative and simple saves you more time during parsing.


Here’s an example log that shows lock “0” acquired successfully by client “0”. When client “2” tries to release lock “0”, they fail to do so because lock “0” is being held by client “0”.

    2023-12-01 16:10:34.000921 [main] INFO  StructuredLogger - : MessageType=REQUEST, ClientID=0, LockID=0, TransactionType=LOCK, TransactionID=0, Result=null, LockStatus=null, Message=null
    2023-12-01 16:10:34.000926 [main] INFO  StructuredLogger - : MessageType=RESPONSE, ClientID=0, LockID=0, TransactionType=LOCK, TransactionID=0, Result=SUCCESS, LockStatus=LOCKED, Message=null
    2023-12-01 16:10:34.000926 [main] INFO  StructuredLogger - : MessageType=REQUEST, ClientID=2, LockID=0, TransactionType=RELEASE, TransactionID=1, Result=null, LockStatus=null, Message=null
    2023-12-01 16:10:34.000928 [main] INFO  StructuredLogger - : MessageType=RESPONSE, ClientID=2, LockID=0, TransactionType=RELEASE, TransactionID=1, Result=FAIL, LockStatus=LOCKED, Message=null

### P Specification for Lock Server
Let's look at the most important rules for the lock server and turn them into P Specificaiton (PSpec):

* Clients can’t acquire locks owned by other clients.
* Locks can’t be released by clients who don’t own them.

Here’s the implementation of the rules stated above: [LockServerCorrect.p](https://github.com/p-org/P/blob/dev/pobserve/Src/PObserve/Examples/LockServerPObserve/src/main/PSpec/LockServerCorrect.p)

    line 13-17 is an example of how to use foreign function, it will be explained later
    line 8-43 contain code for spec CheckLockReleaseCorrectness, it checks the 2 rules specified above
    line 45-68 contain code for spec CheckIfResponseWasRequested, it checks the validity of the response event by checking if the response matches with a request

List the PEvents that are being observed by LockServerCorrect.p so you know what information to parse later:

    * eLockReq
        * Event type:  type tLockReq = (source: Client, clientId: tClientId, lockId: tLockId, rId: int);
        * Client sends this event to lock server to request a specific lock
    * eReleaseReq
        * Event type: type tReleaseReq = (source: Client, clientId: tClientId, lockId: tLockId, rId: int);
        * Client sends this event to lock server to release a specific lock
    * eLockResp
        * Event type: type tLockResp = (status: tLockRespStatus, clientId: tClientId, lockId: tLockId, lockStatus: tLockStatus, rId: int);
        * Lock sends this event back to client as a response to their request with status
    * eReleaseResp
        * Event type: type tReleaseResp = (status: tReleaseRespStatus, clientId: tClientId, lockId: tLockId, lockStatus: tLockStatus, rId: int);
        * Lock sends this event back to client as a response to their request with status