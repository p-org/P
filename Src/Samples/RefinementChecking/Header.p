/*************************************************
Global Type Declarations
*************************************************/
//response payload type
type responseType = (id: int, success: bool);
type requestType = (source: ClientInterface, id:int);

//Interface Types
type ServerClientInterface() = { eRequest }; 
type ClientInterface(ServerClientInterface) = { eResponse };

/*************************************************
Global Event Declarations
*************************************************/
//events between server and client
event eRequest : requestType;
event eResponse: responseType;

//events between server and helper
event eProcessReq: int;
event eReqSuccessful;
event eReqFailed;
