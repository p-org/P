
type SOME_Type = (
    SizeInBytes : SomeType
);
type SomeType = int;

type MessageType = 
(
    List : seq[SOME_Type]
);


event MyEvent : MessageType;



main machine MainMachine
{
    start state Init
    {
    }
}

