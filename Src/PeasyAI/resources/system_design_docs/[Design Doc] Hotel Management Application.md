# Hotel Management Application

## Introduction

The goal of this system is to develop a hotel service booking application that enables the Front Desk to handle client requests, including room reservations, special requests, and cancellations. The application will maintain a centralized database to record room bookings and prevent double booking of rooms.

**Assumptions:**
1. The Front Desk processes requests one at a time in the order they are received.
2. Client ids are unique.
3. The number of rooms is fixed and cannot be altered.
4. Communication between clients and the Front Desk is reliable.

## Components

### Source Components

#### 1. FrontDesk
- **Role:** Manages client requests, including room reservations, special requests, and cancellations.
- **States:** Init, Ready
- **Local state:**
    - `roomAssignments`: maps room number to client name
    - `specialRequests`: maps room number to list of special requests
    - `numRooms`: total number of rooms available
- **Initialization:** Created with the total number of rooms available.
- **Behavior:**
    - Keeps track of room availability and existing reservations.
    - Assigns rooms to clients on reservation requests if available.
    - Validates client identity before processing cancellations and special requests.
    - Communicates with clients regarding their requests and reservations.

### Test Components

#### 2. Client
- **Role:** Makes room reservation requests, submits special requests, and cancels reservations.
- **Local state:**
    - `frontDesk`: reference to the front desk
    - `clientId`: unique client identifier
    - `clientName`: client name
    - `assignedRoom`: room number assigned to this client
- **Initialization:** Created with a reference to the front desk, a unique client ID, and the client's name.
- **Behavior:**
    - Makes room reservation requests.
    - Submits special requests (e.g., preferences for room placement).
    - Cancels room reservations.
    - Waits for responses from the Front Desk.
    - May make additional requests based on responses received.

## Interactions

1. **eRoomReservationRequest**
    - **Source:** Client
    - **Target:** FrontDesk
    - **Payload:** the client's reference, client ID, and client name
    - **Description:** A client requests a room. If successful, the client is assigned a room. If unsuccessful, the client is informed of the failure.
    - **Effects:**
        - If a room is available, the Front Desk assigns it to the client and provides the room number.
        - If no rooms are available, the Front Desk informs the client of the inability to reserve a room.

2. **eRoomReservationResponse**
    - **Source:** FrontDesk
    - **Target:** Client
    - **Payload:** the client ID, the assigned room number, and the status (SUCCESS or FAILURE)
    - **Description:** Front Desk responds to the reservation request with the assigned room number and status.

3. **eCancellationRequest**
    - **Source:** Client
    - **Target:** FrontDesk
    - **Payload:** the client's reference, client ID, client name, and room number
    - **Description:** A client requests to cancel their reservation. The request is processed if the client's name matches the reservation for the given room.
    - **Effects:**
        - If the client's name matches the reservation, the room is freed up, and the client is notified of successful cancellation.
        - If the client's name does not match the reservation, the cancellation is rejected, and the client is informed.

4. **eCancellationResponse**
    - **Source:** FrontDesk
    - **Target:** Client
    - **Payload:** the client ID and the status (SUCCESS or FAILURE)
    - **Description:** Front Desk responds to the cancellation request.

5. **eSpecialRequest**
    - **Source:** Client
    - **Target:** FrontDesk
    - **Payload:** the client's reference, client ID, client name, room number, and the special request details
    - **Description:** A client with an existing reservation submits a special request. The request is recorded if the client is staying in the room mentioned.
    - **Effects:**
        - If the client's name matches the reservation for the specified room, the request is noted, and the client is informed of its successful recording.
        - If the client's name does not match the reservation, the request is discarded, and the client is informed.

6. **eSpecialResponse**
    - **Source:** FrontDesk
    - **Target:** Client
    - **Payload:** the client ID and the status (SUCCESS or FAILURE)
    - **Description:** Front Desk responds to the special request.

## Specifications

1. **NoDoubleBooking** (safety property):
   Between a successful eRoomReservationResponse assigning a room and a successful eCancellationResponse freeing it, no other eRoomReservationRequest may result in that same room being assigned to a different client.

2. **ReservationIntegrity** (safety property):
   An eSpecialResponse must never alter room assignments. While an eRoomReservationRequest or eCancellationRequest is being processed, existing room assignments must remain unchanged.

## Test Scenarios

1. 1 front desk (3 rooms), 2 clients — both clients reserve rooms successfully.
2. 1 front desk (1 room), 2 clients — second client's reservation fails due to no availability.
3. 1 front desk (2 rooms), 1 client — client reserves a room, then cancels, then reserves again.
4. 1 front desk (2 rooms), 2 clients — one client tries to cancel a reservation they do not hold.
