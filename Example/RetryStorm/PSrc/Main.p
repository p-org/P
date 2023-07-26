event eStart;
event eRequest: (id: int, client: Client);
event eResponse: (id: int);
event eServerRun;
event eClientRun;

module Module = { Client, Server };
