/*********************************************************************************************
The AbstractServer machine implements a sound abstraction of the Server machine.
choose() randomly chooses to pass or fail the request, capturing the behavior of the server
where it interacts with the Helper machine to determine if the request is successful or failed.
**********************************************************************************************/
// Interface exposed by server machine to the client machine
interface iServer() receives eRequest;

machine AbstractServer
{
  start state Init {
    on eRequest do (payload: tRequest){
        if($)
            send payload.source, eResponse, (rId = payload.rId, status = SUCCESS);
        else
            send payload.source, eResponse, (rId = payload.rId, status = ERROR);
    }
  }
}