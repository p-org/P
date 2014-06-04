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
\begin{CoffeeShop}
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
\end{CoffeeShop}

model machine Customer
\begin{Customer}
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
\end{Customer}

