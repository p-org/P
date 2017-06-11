/******************************************
ListSpec: Ensures that the client of the list data structure performs legal operations.
******************************************/

spec ListClientSpec observes eDSOperation
{
    var list: seq[data];

    start state Init {
        on eDSOperation do (payload: DSOperationType){
            if(payload.op == ADD)
            {
                list += (sizeof(list), payload.val);
            }
            else if(payload.op == READ)
            {
                assert(payload.val as int < sizeof(list));
                
            }
            else if(payload.op == REMOVE)
            {
                assert(payload.val as int < sizeof(list));
                list -= (payload.val as int);
            }
        }
    }
}
