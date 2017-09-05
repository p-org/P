/*************************
The TestDriver1 machine creates a Data structure client and sets up the testing problem
*************************/
machine TestDriver1 
receives;
sends;
{
    start state Init {
        entry {
            //perform 100 operations
            new SMRClientInterface(100);
            raise halt;
        }
    }
}