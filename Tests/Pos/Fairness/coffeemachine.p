event MsgOrderReceived;
event MsgPay;
event MsgPickup;
event MsgNewOrder;
event MsgReady;
event MsgPaid;
event MsgCustomerPlacesOrder;
event Unit;
event MsgInformCoffeeMachine:mid;
event MsgInformCustomerMachine:mid;

main model machine CoffeeShop
{
        var _customer, _server, _coffeemachine:mid;
        start state init
        {
            on Unit goto WaitForDone;
            entry
            {
				new Dummy();
				_server = new Server();
                _customer = new Customer(_server);
                _coffeemachine = new CoffeeMachine(_server);
                send(_server, MsgInformCustomerMachine, _customer);
                send(_server, MsgInformCoffeeMachine, _coffeemachine);
                send(_customer, MsgCustomerPlacesOrder);
                send(_customer, MsgCustomerPlacesOrder);
                send(_customer, MsgCustomerPlacesOrder);
                send(_customer, MsgCustomerPlacesOrder);
                raise(Unit);
            }
        }

        stable state WaitForDone
        {
        }
}

model machine Customer
{
        var _server:mid;

        start state _Init {
			entry { _server = (mid) payload; raise(Unit); }
            on Unit goto OrderNotPlaced;
        }

        stable state OrderNotPlaced
        {
			on MsgCustomerPlacesOrder goto PlacingOrder
            {
				send (_server, MsgNewOrder);
            };
        }

        state PlacingOrder
        {
			defer MsgCustomerPlacesOrder;
            on MsgOrderReceived goto OrderPlaced;
        }

        state OrderPlaced
        {
			defer MsgCustomerPlacesOrder;
            on MsgPay goto OrderPaid
            {
				send (_server, MsgPaid);    
            };
        }

        state OrderPaid
        {
			defer MsgCustomerPlacesOrder;
            on MsgPickup goto OrderNotPlaced;
        }
}

model machine Server
{
        var _customer:mid;
        var _coffeeMachine:mid;

        start state InitCustomer
        {
            on MsgInformCustomerMachine goto InitCoffee
            {
                _customer = (mid)payload;
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
					send (_customer, MsgOrderReceived);
                };
        }

        state ReceivedOrder
        {
            on MsgNewOrder goto MakingOrder
            {
				send(_coffeeMachine, MsgNewOrder);
            };
        }

        model fun bar()
        {
            send (_customer, MsgPay);
        }

        state MakingOrder
        {
            on MsgReady goto OrderReady
            {
				bar();
            };
        }

        state OrderReady
        {
            on MsgPaid goto WaitingOrder
            {
				send (_customer, MsgPickup);
            };
        }
} 

model machine CoffeeMachine
{
        var _server:mid;

        start state _Init {
			entry { _server = (mid) payload; raise(Unit); }
            on Unit goto WaitingOrder;
        }

        stable state WaitingOrder
        {
            on MsgNewOrder goto WaitingOrder
            {
				send (_server, MsgReady);
            };
        }
}

monitor Dummy {
	start state Init {}
}