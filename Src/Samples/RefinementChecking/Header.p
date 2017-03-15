/*************************************************
Global Type Declarations
*************************************************/
//response payload type
<<<<<<< HEAD
type responseType : (id: int, success: bool);
=======
type responseType : (id: int, success: bool)
>>>>>>> 67e07c7449eeb61e3ee19fa58dbe9632d2861fb7
type requestType : (source: ClientInterface, id:int);

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
<<<<<<< HEAD
event eReqFailed;
=======
event eReqFailed;
>>>>>>> 67e07c7449eeb61e3ee19fa58dbe9632d2861fb7
