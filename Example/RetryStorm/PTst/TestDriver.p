machine TestCase {
    start state Init {
        entry {
            var server: Server;
            var client: Client;
            var _ServerPayload: (maxQPS: int);
            var _ClientPayload: (maxQPS: int, server: Server);

            _ServerPayload.maxQPS = 2;
            server = new Server(_ServerPayload);
            send server, eStart;

            _ClientPayload.maxQPS = 2;
            _ClientPayload.server = server;
            client = new Client(_ClientPayload);
            send client, eStart;
        }
    }
}
