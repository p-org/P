// In this example, we have a bunch of clients and servers.
// Initially, no client is connected to a server and each server holds a resource/semaphore.
// A client can attempt to connect to a server.
// A server may send a connect ack if its resource is free, else can return unavailable to the client.
// After using the resource, the client can disconnect from the server.


type tConnect = (client: Client, clientId: int, serverId: int);
type tDisconnect = (client: Client, clientId: int, serverId: int);

type tConnectAck = (server: Server, serverId: int, clientId: int);
type tUnavailable = (server: Server, serverId: int, clientId: int);

// events from client to server
event eConnect    : tConnect;
event eDisconnect : tDisconnect;

// events from server to client
event eConnectAck    : tConnectAck;
event eUnavailable   : tUnavailable;
