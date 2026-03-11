# Client Server

## Introduction

The goal of this system is to model a simple client-server interaction where clients send requests to a server and the server processes those requests and sends back responses. This is the most basic distributed system pattern.

**Assumptions:**
1. The server processes requests one at a time in the order they are received.
2. Communication between clients and server is reliable.
3. The server does not fail.
4. Each client sends a fixed number of requests.

## Components

### Source Components

#### 1. Server
- **Role:** Receives requests from clients, processes them, and sends responses.
- **States:** Init, Ready
- **Local state:**
    - `requestCount`: number of requests processed
- **Initialization:** No external configuration needed.
- **Behavior:**
    - Receives requests from clients.
    - Processes each request and sends a response back to the requesting client.
    - Keeps count of the number of requests processed.

### Test Components

#### 2. Client
- **Role:** Sends requests to the server and tracks responses.
- **States:** Init, Sending, Done
- **Local state:**
    - `server`: reference to the server
    - `numRequests`: number of requests to send
    - `requestsSent`: count of requests sent so far
    - `responsesReceived`: count of responses received
- **Initialization:** Created with a reference to the server and the number of requests to send.
- **Behavior:**
    - Sends requests to the server with a unique request identifier.
    - Waits for the server's response before sending the next request.
    - Keeps track of responses received.

## Interactions

1. **eRequest**
    - **Source:** Client
    - **Target:** Server
    - **Payload:** the requesting client's reference and a unique request ID
    - **Description:** Client sends a request to the server.
    - **Effects:**
        - Server processes the request and prepares a response.

2. **eResponse**
    - **Source:** Server
    - **Target:** Client
    - **Payload:** the request ID and whether the request succeeded
    - **Description:** Server sends a response back to the client after processing the request.
    - **Effects:**
        - Client receives the response and may send the next request.

## Specifications

1. **EveryRequestGetsResponse** (safety property):
   Every eRequest sent by a client must eventually be matched by exactly one eResponse from the server with the same requestId. The server must not drop or duplicate eResponse messages.

## Test Scenarios

1. 1 server, 1 client (3 requests) — a single client sends multiple requests to the server sequentially.
2. 1 server, 3 clients (2 requests each) — multiple clients send requests to the same server concurrently.
3. 1 server, 3 clients (different request counts: 1, 3, 5) — clients with different request counts interact with the server.
