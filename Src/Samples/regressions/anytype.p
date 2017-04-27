//regression: structural typing, static error.

//declare interface type

eventset es = { E1 };
event E1 : any<es>;
event E2;
type I1((a: int, b: any)) = {E1};
type I2((a: bool, b: int)) = {E1, E2};

machine M : I1 {
    var i: I2;
    start state Init {
        entry (payload: (a:int, b: any)) {
          send this, E1, i;
        }
    }
}