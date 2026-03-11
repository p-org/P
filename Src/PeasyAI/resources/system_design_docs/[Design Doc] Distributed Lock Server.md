# Distributed Lock Server

## Introduction

The goal of this system is to model a distributed lock server, which is used to manage access to shared resources in a distributed environment. The lock server ensures that only one client can hold a lock on a resource at any given time, preventing conflicts and ensuring consistency.

**Assumptions:**
1. The system allows multiple clients to request and release locks concurrently.
2. The lock server is assumed to be reliable; it does not fail, and it always responds to client requests.
3. The system does not support lock server replication; there is only a single lock server managing all locks.
4. The system models reliable communication between clients and the lock server.

## Components

### Source Components

#### 1. LockServer
- **Role:** Manages locks on resources, granting and releasing them in response to client requests.
- **States:** Init, Ready
- **Local state:**
    - `lockTable`: maps resource ID to the client currently holding the lock
    - `waitQueues`: maps resource ID to a queue of clients waiting for the lock
- **Initialization:** No external configuration needed.
- **Behavior:**
    - Processes lock requests and release requests from clients in the order they are received.
    - Grants a lock if it is available, otherwise queues the request.
    - Releases a lock when requested by the client holding it, and if there are pending requests for the same lock, grants the lock to the next client in the queue.

### Test Components

#### 2. Client
- **Role:** Requests and releases locks on specific resources.
- **Local state:**
    - `lockServer`: reference to the lock server
    - `clientId`: unique client identifier
    - `resourceId`: the resource this client wants to lock
- **Initialization:** Created with a reference to the lock server, a unique client ID, and the resource ID it wants to lock.
- **Behavior:**
    - Sends lock requests to the Lock Server for a specific resource.
    - After acquiring a lock, performs operations on the resource.
    - Sends a release request to the Lock Server after the operations are complete.

## Interactions

1. **eLockRequest**
    - **Source:** Client
    - **Target:** LockServer
    - **Payload:** the client's reference, client ID, and resource ID
    - **Description:** Client requests a lock on a specific resource.
    - **Effects:**
        - If the lock is available, grant it to the client.
        - If the lock is held by another client, queue the request.

2. **eLockResponse**
    - **Source:** LockServer
    - **Target:** Client
    - **Payload:** the client ID, resource ID, and lock status (granted or queued)
    - **Description:** Lock server responds to the client's lock request.
    - **Effects:**
        - Client either proceeds with operations on the resource if the lock is granted or waits if queued.

3. **eReleaseRequest**
    - **Source:** Client
    - **Target:** LockServer
    - **Payload:** the client's reference, client ID, and resource ID
    - **Description:** Client sends a request to release the lock on a specific resource.
    - **Effects:**
        - Lock Server releases the lock and grants it to the next client in the queue if any.

4. **eReleaseResponse**
    - **Source:** LockServer
    - **Target:** Client
    - **Payload:** the client ID, resource ID, and release status
    - **Description:** Lock Server confirms the release of the lock.

## Specifications

1. **MutualExclusion** (safety property):
   Between an eLockResponse granting a lock on a resource and the corresponding eReleaseRequest from that client, no other eLockResponse may grant the same resource to a different client.

2. **DeadlockFreedom** (liveness property):
   Every eLockRequest must eventually result in an eLockResponse granting the lock, provided clients eventually send eReleaseRequest for resources they hold.

## Test Scenarios

1. 1 lock server, 3 clients, 1 resource — multiple clients request the same lock concurrently, and the lock server handles the contention.
2. 1 lock server, 3 clients, 3 resources — clients request and release locks on different resources, with no contention.
