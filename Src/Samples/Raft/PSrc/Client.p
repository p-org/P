// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;

// namespace Raft
// {
machine Client
{
    // machine Cluster;
    // int LatestCommand;
    // int Counter;
    var Cluster: machine;
    var LatestCommand: int;
    var Counter: int;


    start state Init
    {
        entry
        {
            LatestCommand = -1;
            Counter = 0;
        }
        on CConfigureEvent do (payload: machine) {
            Configure(payload);
        } 
        on LocalEvent goto PumpRequest;
    }

    fun Configure(payload: machine)
    {
        Cluster = payload;
        raise LocalEvent;
    }

    state PumpRequest
    { 
        entry
        {
            LatestCommand = ChooseVal();
            Counter = Counter + 1;
            //Logger.WriteLine("\n [Client] new request " + this.LatestCommand + "\n");
            print "\n\n\n[Client] new request {0}\n", LatestCommand;
            send Cluster, Request, (Client=this, Command=LatestCommand);
        }    

        on Response do {
            ProcessResponse();
        }
        on LocalEvent goto PumpRequest;
    }


    fun ChooseVal() : int {
        // return a random value between 0 - 100
        return Counter;
        // var index : int;
        // index = 0;
        // while(index < 100)
        // {
        //     if($)
        //     {
        //         return index;
        //     }
        //     index = index + 1;
        // }

        // return index;
    }

    fun ProcessResponse()
    {
        print "In ProcessResponse of Client";
        if (Counter == 100)
        {

            send Cluster, ShutDown;
            raise halt;
        }
        else
        {
            raise LocalEvent;
        }
    }
}

