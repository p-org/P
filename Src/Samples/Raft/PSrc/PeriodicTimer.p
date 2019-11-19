
machine PeriodicTimer
{
    var Target: machine;

    start state Init
    {
        on PConfigureEvent do (payload: machine) {
            Configure(payload);
            print "Configured PeriodicTimer {0} for {1}", this, payload;
        }
        on PStartTimer goto Active;
    }

    fun Configure(payload: machine)
    {
        Target = payload;
    }

    state Active
    {
        entry
        {
            print "Entered Active for PeriodicTimer {0}", this;
            send this, PTickEvent;
        }

        on PTickEvent do Tick;
        on PCancelTimer goto Inactive;
        defer PStartTimer;
    }

    fun Tick()
    {
        if ($)
        {
            //this.Logger.WriteLine("\n [PeriodicTimer] " + this.Target + " | timed out\n");
            print "[PeriodicTimer] {0} | timed out\n", Target;
            send Target, PTimeout;
        } 
        raise PCancelTimer;
    }

    state Inactive
    {
        on PStartTimer goto Active;
        defer PCancelTimer, PTickEvent;
    }
}


