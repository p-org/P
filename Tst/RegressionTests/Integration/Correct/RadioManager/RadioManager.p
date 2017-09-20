event Done;
event Interrupt;
event Maybe;
event No;
event Notify;
event TimerFired;
event TurnOff;
event TurnOn;
event Yes;
event __unit;
event __Marker;
machine Main
{
    fun _anon__goto_function(_anon__goto_function_payload: null)
    {
    }
    fun _anon_CheckingIfEnabled_Entry()
    {
        IsEnabled();
    }
    fun CheckingIfEnabled_Entry(payload: null)
    {
        _anon_CheckingIfEnabled_Entry();
    }
    fun _anon_CheckingIfEnabled_Exit()
    {
    }
    fun CheckingIfEnabled_Exit(payload: null)
    {
        _anon_CheckingIfEnabled_Exit();
    }
    fun _anon_Off_Entry()
    {
        SetOff();
        NotifyOff();
    }
    fun Off_Entry(payload: null)
    {
        _anon_Off_Entry();
    }
    fun _anon_Off_Exit()
    {
        var __res: bool;
        __res = StopTimer();
        if(__res)
        {
            receive
            {
                case TimerFired: (payload: null) {
                }
            }
            
        }
        else
        {
            
        }
    }
    fun Off_Exit(payload: null)
    {
        _anon_Off_Exit();
    }
    fun _anon_Off_do_Interrupt(_anon_Off_do_Interrupt_payload: null): null
    {
        SetOff();
        return (null);
    }
    fun _anon_PreparingForStateChangeToOn_Entry()
    {
        SetOff();
        raise __unit;
    }
    fun PreparingForStateChangeToOn_Entry(payload: null)
    {
        _anon_PreparingForStateChangeToOn_Entry();
    }
    fun _anon_PreparingForStateChangeToOn_Exit()
    {
        var __res: bool;
        __res = StopTimer();
        if(__res)
        {
            receive
            {
                case TimerFired: (payload: null) {
                }
            }
            
        }
        else
        {
            
        }
    }
    fun PreparingForStateChangeToOn_Exit(payload: null)
    {
        _anon_PreparingForStateChangeToOn_Exit();
    }
    fun _anon_PreparingForStateChangeToOn_do_Interrupt(_anon_PreparingForStateChangeToOn_do_Interrupt_payload: null): null
    {
        StopPreparation();
        return (null);
    }
    fun _anon_StartPreparation_Entry()
    {
        StartPreparation();
    }
    fun StartPreparation_Entry(payload: null)
    {
        _anon_StartPreparation_Entry();
    }
    fun _anon_StartPreparation_Exit()
    {
    }
    fun StartPreparation_Exit(payload: null)
    {
        _anon_StartPreparation_Exit();
    }
    fun _anon_StopPreparation_Entry()
    {
        StopPreparation();
    }
    fun StopPreparation_Entry(payload: null)
    {
        _anon_StopPreparation_Entry();
    }
    fun _anon_StopPreparation_Exit()
    {
    }
    fun StopPreparation_Exit(payload: null)
    {
        _anon_StopPreparation_Exit();
    }
    fun _anon_StopPreparation_do_Done(_anon_StopPreparation_do_Done_payload: null): null
    {
        pop;
        return (null);
    }
    //model fun IsEnabled()
	fun IsEnabled()
    {
    }
    fun NotifyOff()
    {
    }
    fun NotifyOn()
    {
    }
    fun SetOff()
    {
    }
    fun SetOn()
    {
    }
    fun StartPreparation()
    {
    }
    fun StopPreparation()
    {
    }
    fun StopTimer(): bool
    {
        if($)
        {
            return (true);
            
        }
        else
        {
            return (false);
            
        }
    }
    fun TurnOff()
    {
    }
    fun TurnOn()
    {
    }
     state PreparingForStateChangeToOn {
        entry (payload: null) {
            PreparingForStateChangeToOn_Entry(payload);
        }
        exit {
            PreparingForStateChangeToOn_Exit(null);
        }
        on Interrupt do (payload: null) {
            payload = _anon_PreparingForStateChangeToOn_do_Interrupt(payload);
        }
        defer Notify;
        defer TurnOff;
        defer TurnOn;
        on Done goto Off with (payload: null) {
            _anon__goto_function(payload);
        }
        on __unit push PrepareForStateChangeMachine.StartPreparation;
    }
    start  state Off {
        entry (payload: null) {
            Off_Entry(payload);
        }
        exit {
            Off_Exit(null);
        }
        on Interrupt do (payload: null) {
            payload = _anon_Off_do_Interrupt(payload);
        }
        ignore TurnOff;
        on Notify goto Off with (payload: null) {
            _anon__goto_function(payload);
        }
        on TurnOn goto CheckingIfEnabled with (payload: null) {
            _anon__goto_function(payload);
        }
    }
     state CheckingIfEnabled {
        entry (payload: null) {
            CheckingIfEnabled_Entry(payload);
        }
        exit {
            CheckingIfEnabled_Exit(null);
        }
        on Maybe goto PreparingForStateChangeToOn with (payload: null) {
            _anon__goto_function(payload);
        }
        on No goto Off with (payload: null) {
            _anon__goto_function(payload);
        }
        on Yes goto PreparingForStateChangeToOn with (payload: null) {
            _anon__goto_function(payload);
        }
    }
    group PrepareForStateChangeMachine {
         state StopPreparation {
            entry (payload: null) {
                StopPreparation_Entry(payload);
            }
            exit {
                StopPreparation_Exit(null);
            }
            on Done do (payload: null) {
                payload = _anon_StopPreparation_do_Done(payload);
            }
        }
         state StartPreparation {
            entry (payload: null) {
                StartPreparation_Entry(payload);
            }
            exit {
                StartPreparation_Exit(null);
            }
            on Done goto PrepareForStateChangeMachine.StopPreparation with (payload: null) {
                _anon__goto_function(payload);
            }
        }
    }
}
