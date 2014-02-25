event MsgOrderReceived;
event MsgPay:mid;
event MsgPickup;
event MsgNewOrder:mid;
event MsgcfmNewOrder:(mid,mid);
event MsgReady:mid;
event MsgPaid:mid;
event MsgCustomerPlacesOrder:mid;
event Unit;
event MsgInformCoffeeMachine:mid;
event MsgInformCustomerMachine;
event CanIorder:mid;
event MsgGatekeeperReply:int;
event MsgRedirectContinueWithCustomerOrder:mid;
event MsgContinueWithCustomerOrder:mid;
event MsgRedirectCustomerPlacesOrder:mid;
event MsgServerDone;
event MsgReadyForNewOrder:mid;

main fair model machine CoffeeShop
{
        var _customer0,_customer1,_server0,_server1,_coffeemachine0,_serverGateKeeper0:mid;
        start state init
        {
            on Unit goto WaitForDone;
            entry
            {
				_serverGateKeeper0 = new ServerGateKeeper();
                _server0 = new Server(_serverGateKeeper0);
				_server1 = new Server(_serverGateKeeper0);
				_customer0 = new Customer((_server0, _serverGateKeeper0));
				_customer1 = new Customer((_server0, _serverGateKeeper0));
                _coffeemachine0 = new CoffeeMachine();
				send(_server0, MsgInformCustomerMachine);
                send(_server0, MsgInformCoffeeMachine, _coffeemachine0);
                send(_server1, MsgInformCustomerMachine); 
                send(_server1, MsgInformCoffeeMachine, _coffeemachine0 );
                send(_customer0, MsgCustomerPlacesOrder,_server0);
                //send(_customer0, MsgCustomerPlacesOrder,_server1);
                send(_customer1, MsgCustomerPlacesOrder,_server0);				
                //send(_customer1, MsgCustomerPlacesOrder,_server1);
                raise(Unit);
            }
        }

        stable state WaitForDone		
        {
        }
}

fair model machine Customer
{
        var _server0:mid;
		var _server1:mid;
		var _serverGateKeeper:mid;

        start state _Init {			
			entry { 
			_server0 = ((mid,mid)) payload[0]; 
			_serverGateKeeper = ((mid,mid)) payload[1]; raise(Unit); 			
			}
            on Unit push OrderNotPlaced;
			
        }

        stable state OrderNotPlaced
        {
			defer MsgcfmNewOrder;
			defer MsgOrderReceived;
			defer MsgPickup;
			on MsgCustomerPlacesOrder goto ValidateReply
            {	
				send(_serverGateKeeper, CanIorder, this);				
            };
        }
		
		state ValidateReply
		{
			on MsgGatekeeperReply goto RedirectNextState
			{
				if((int) payload == 1)
				{
					send(this, MsgRedirectContinueWithCustomerOrder, _server0);
				}
				else
				{
					send(this, MsgRedirectCustomerPlacesOrder, _server0);
				}
			};
		}
		
		state RedirectNextState
		{
			on MsgRedirectContinueWithCustomerOrder goto CanPlaceOrder
			{
				send(this, MsgContinueWithCustomerOrder, _server0);
			};

			on MsgRedirectCustomerPlacesOrder goto OrderNotPlaced
			{
				send (this, MsgCustomerPlacesOrder, _server0);
			};
		}
		
		 state CanPlaceOrder
        {
			defer MsgcfmNewOrder;
			defer MsgOrderReceived;
			defer MsgPickup;
			on MsgContinueWithCustomerOrder goto  PlacingOrder
            {	
				send ( (mid)payload, MsgNewOrder, this);				
            };
        }
		
        state PlacingOrder
        {
			defer MsgCustomerPlacesOrder;
			defer MsgPay;
			defer MsgPickup;
			on MsgOrderReceived goto OrderPlaced;			
        }

        state OrderPlaced
        {
			defer MsgCustomerPlacesOrder;
			defer MsgOrderReceived;
			defer MsgPickup;
            on MsgPay goto OrderPaid
            {
				send ((mid)payload, MsgPaid, this);  
            };
        }
		
        state OrderPaid
        {
			defer MsgCustomerPlacesOrder;
			defer MsgPay;
			defer MsgOrderReceived;
            on MsgPickup goto OrderNotPlaced;
        }
}

fair model machine ServerGateKeeper
{
	var CountBeenProcessed:int;
	
	start state _Init {
			entry { 	
				CountBeenProcessed = 0;
				raise(Unit); 
			}
            on Unit goto AcceptNewOrder;
        }
		
    stable state AcceptNewOrder{
		on CanIorder goto AcceptNewOrder
		{
			if(CountBeenProcessed == 0 )
			{
				CountBeenProcessed = 1;
				send((mid) payload,MsgGatekeeperReply, 1); 
			}
			else
			{
				send((mid) payload,MsgGatekeeperReply, 0); 
			}
		};
		
		on MsgServerDone goto AcceptNewOrder
		{
			if(CountBeenProcessed == 1)
			{
				CountBeenProcessed = 0;
			}
		};
	}
}

fair model machine Server
{
    var _coffeeMachine:mid;
	var _serverGateKeeper0:mid; 

	start state _Init {			
			entry {  
			_serverGateKeeper0 = (mid) payload; raise(Unit); 			
			}
            on Unit push InitCustomer;		
    }

	state InitCustomer {
		on MsgInformCustomerMachine goto InitCoffee
        {		 
		};
    }		            

    state InitCoffee
    {
        on MsgInformCoffeeMachine goto WaitingOrder
		{
            _coffeeMachine = (mid)payload;
        };
    }

    stable state WaitingOrder
    {
      	on MsgNewOrder goto ReceivedOrder
       	{
			send((mid) payload, MsgOrderReceived); 			
			send (this, MsgNewOrder, (mid) payload);
       	};
    }

    state ReceivedOrder
    {
		on MsgNewOrder goto MakingOrder
		{
			send(_coffeeMachine, MsgcfmNewOrder, (this, (mid)payload));
        };
    }

    state MakingOrder
    {
		on MsgReady goto OrderReady
		{
			send ((mid) payload, MsgPay, this);		
        };
    }

	state OrderReady
	{
		on MsgPaid goto WaitingOrder 
		{			
			send ((mid) payload, MsgPickup);
			send (_serverGateKeeper0, MsgServerDone);		
		};
	}
	
	state SendDoneMessage
	{
		defer MsgNewOrder;
		on MsgReadyForNewOrder goto WaitingOrder
		{
			send (_serverGateKeeper0, MsgServerDone);		
		};
	}
} 

fair model machine CoffeeMachine
{
        start state _Init {
			entry { 		
			raise(Unit); 
			}
            on Unit goto WaitingOrder;
        }

        stable state WaitingOrder
        {	
            on MsgcfmNewOrder goto WaitingOrder
            {					
				send((((mid,mid))payload)[0], MsgReady,(((mid,mid)) payload)[1]); 				
            };
        }		
}