//regression: structural typing, static error.

//declare interface type

event E1;
event E2;
type I1((a: int, b: any)) = {E1, E2};
type I2((a: bool, b: int)) = {E1, E2};

machine M : I1,I2  {
    start state Init {
        entry (payload: (a:int, b: any)) {

        }
    }
}