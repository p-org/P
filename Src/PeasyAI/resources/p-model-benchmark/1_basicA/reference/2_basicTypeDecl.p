// Complete the TODOs in the following code, don't modify the machines. 
// Only write code, no commentary:

machine Client {
    start state Init {

    }
}

machine Bank {
    start state Init {
        
    }
}

machine LightSwitch {
    start state Off {
        
    }
}


// TODO: Create a type that represents the message sent when a light switch is flipped. This message carries a reference to the LightSwitch that was flipped and the position it is in as a boolean."

// TODO: Create a type that represents the message sent the Client requests to withdraw an ammount from the bank. It contains a reference to the Client, the account id, and the ammount to withdraw "

// ========= Everything below this should be generated correctly by the model ==============
type tSwitchToggledReq = (source: LightSwitch, position: bool);

type tBankWithdrawReq = (source: Client, accountId: string, amount: int);


