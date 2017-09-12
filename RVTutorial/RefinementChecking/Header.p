//Events between client and server
event eRequest : requestType;
event eResponse: responseType;

//Events between server and helper
event eProcessReq: int;
event eReqSuccessful;
event eReqFailed;

//Payload types
type requestType = (source: ClientInterface, id:int);
type responseType = (id: int, success: bool);

//Interface types
interface ServerClientInterface() receives eRequest; 
interface ClientInterface(ServerClientInterface) receives eResponse;
interface ServerHelperInterface() receives eReqSuccessful, eReqFailed;
interface HelperInterface(ServerHelperInterface) receives eProcessReq;
