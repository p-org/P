//Defines the transaction type 
type IC() = { E1, E2 };
type IC1() = { E2 };
event E2;
type TransactionType =
(
    source: IC,
    op1: int,
    op2: int
);

event E1: TransactionType;

machine Test {
    start state Init {
        entry {
            var x : IC1;
            send this, E1, (source = x as IC, op1 = 0, op2 = 0);
        }
    }
}