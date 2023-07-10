using System.Collections.Generic;
using System.Text.Json;

namespace PChecker.Actors.Logging
{
    internal sealed class JsonWriter
    {
        /// <summary>
        /// Enum representing the different log types the JSON error trace logs.
        /// Referenced from PLogFormatter.cs and ActorRuntimeLogTextFormatter.cs
        /// to see what those formatter logs. Check IActorRuntimeLog.cs to see
        /// each log types' description and when they are invoked.
        /// </summary>
        public enum LogTypes
        {
            AssertionFailure,
            CreateActor,
            CreateStateMachine,
            CreateMonitor,
            CreateTimer,
            DefaultEventHandler,
            DequeueEvent,
            ExceptionHandled,
            ExceptionThrown,
            GotoState,
            Halt,
            MonitorProcessEvent,
            MonitorRaiseEvent,
            MonitorStateTransition,
            PopState,
            PopStateUnhandledEvent,
            PushState,
            RaiseEvent,
            ReceiveEvent,
            SendEvent,
            StateTransition,
            StopTimer,
            StrategyDescription,
            WaitEvent,
            WaitMultipleEvents
        }

        /// <summary>
        /// Enum representing the possible attributes in the details dictionary,
        /// which represents the data associated with a specific log type. All of
        /// the following are available or expanded parameters associated with an
        /// IActorRuntime method. Naming for them is mostly the same except some
        /// are changed for simplicity.
        /// I.e., for OnRaiseEvent(ActorId id, string, stateName, Event e), it
        /// will have attributes id, state (simplified from stateName, event
        /// (simplified from eventName within Event e), and payload (in Event e). 
        /// </summary>
        public enum DetailAttr
        {
            /// <summary>
            /// The text log from PLogFormatter. Removes the log tags.
            /// I.e., no <SomeLog> in the beginning.
            /// Available for all log types.
            /// </summary>
            log,

            /// <summary>
            /// The actor id.
            /// </summary>
            id,

            /// <summary>
            /// The error message if an error occurs.
            /// </summary>
            error,

            /// <summary>
            /// The event name. I.e. eWithDrawReq, etc...
            /// @event to bypass C# reserved identifier event
            /// </summary>
            @event,

            /// <summary>
            /// Name of creator.
            /// </summary>
            creatorName,

            /// <summary>
            /// Type of creator.
            /// </summary>
            creatorType,

            /// <summary>
            /// Information about timer.
            /// </summary>
            timerInfo,

            /// <summary>
            /// Amount of time to wait before sending the first timeout event.
            /// </summary>
            timerDueTime,

            /// <summary>
            /// Time interval between timeout events.
            /// </summary>
            timerPeriod,

            /// <summary>
            /// The actor id that owns the timer.
            /// Available for log type CreateTimer and StopTimer.
            /// </summary>
            source,

            /// <summary>
            /// The state associated with an event, machine, etc...
            /// </summary>
            state,

            /// <summary>
            /// The initial state associated with an event, machine, etc...
            /// Available for log types GotoState, PopState, PushState
            /// </summary>
            startState,

            /// <summary>
            /// The initial state associated with an event, machine, etc...
            /// Available for log types GotoState, PopState, PushState
            /// </summary>
            endState,

            /// <summary>
            /// Payload of an event. Represented in dictionary object.
            /// I.e.
            /// {
            ///     "source": "Client(4)",
            ///     "accountId": "0",
            ///     "amount": "52",
            ///     "rId": "1"
            /// }
            /// </summary>
            payload,

            /// <summary>
            /// The action being executed.
            /// Available for log type ExceptionHandled and ExceptionThrown.
            /// </summary>
            action,

            /// <summary>
            /// The name of the exception.
            /// Available for log type ExceptionHandled and ExceptionThrown.
            /// </summary>
            exception,

            /// <summary>
            /// Approximate size of the inbox.
            /// Available for log type Halt.
            /// </summary>
            haltInboxSize,

            /// <summary>
            /// Boolean representing whether an actor was waiting for one or more events
            /// Available for log type ReceiveEvent.
            /// </summary>
            wasBlocked,

            /// <summary>
            /// Name of sender.
            /// Available for log type SendEvent.
            /// </summary>
            sender,

            /// <summary>
            /// Id of target actor.
            /// Available for log type SendEvent.
            /// </summary>
            target,

            /// <summary>
            /// The id for the send operation.
            /// Available for log type SendEvent.
            /// </summary>
            opGroupId,

            /// <summary>
            /// Boolean representing whether the target actor was halted.
            /// Available for log type SendEvent.
            /// </summary>
            isTargetHalted,

            /// <summary>
            /// Boolean representing whether it's a state entry to exit.
            /// Available for log types StateTransition and MonitorStateTransition.
            /// </summary>
            isEntry,

            /// <summary>
            /// Boolean representing whether monitor is in hot or cold state.
            /// Available for log type MonitorStateTransition.
            /// </summary>
            isInHotState,

            /// <summary>
            /// The type of the event being waited for.
            /// Available for log type WaitEvent.
            /// </summary>
            eventType,

            /// <summary>
            /// The type of events being waited for.
            /// Available for log type WaitMultipleEvents.
            /// </summary>
            eventTypes,

            /// <summary>
            /// Name of monitor.
            /// Available for log types CreateMonitor, MonitorProcessEvent,
            /// MonitorRaiseEvent, and MonitorStateTransition
            /// </summary>
            monitor,

            /// <summary>
            /// Name of strategy used.
            /// Available for log type StrategyDescription.
            /// </summary>
            strategy,

            /// <summary>
            /// Information about the scheduling strategy.
            /// Available for log type StrategyDescription.
            /// </summary>
            strategyDescrption
        }

        /// <summary>
        /// List of dictionary objects with attributes logType and details.
        /// Each item in the list represents an individual log.
        /// </summary>
        private readonly List<Dictionary<string, object>> _logs;

        /// <summary>
        /// Dictionary with keys logType and details. Represents an individual log.
        /// </summary>
        private Dictionary<string, object> _log;

        /// <summary>
        /// Dictionary with possible keys from DetailAttr. Represents details
        /// associated with a specific log.
        /// </summary>
        private Dictionary<string, object> _details;

        /// <summary>
        /// Initializes the Writer. Create empty _logs, _log, and _details objects.
        /// </summary>
        public JsonWriter()
        {
            _logs = new List<Dictionary<string, object>>();
            _log = new Dictionary<string, object>();
            _details = new Dictionary<string, object>();
        }

        /// <summary>
        /// Adds a detail associated with a log to the _details dictionary.
        /// </summary>
        /// <param name="key">Key is of type DetailAttr.</param>
        /// <param name="value">Information of the detail added.</param>
        public void AddDetail(DetailAttr key, object value)
        {
            _details.Add(key.ToString(), value);
        }

        /// <summary>
        /// Adds log text to details dictionary.
        /// </summary>
        /// <param name="log">The log text.</param>
        public void AddLog(string log)
        {
            _details.Add(DetailAttr.log.ToString(), log);
        }

        /// <summary>
        /// Add the log type to _log dictionary.
        /// </summary>
        /// <param name="logType">The log type</param>
        public void AddLogType(LogTypes logType)
        {
            _log.Add("logType", logType.ToString());
        }

        /// <summary>
        /// Add _log to _logs and resets the Writer for next available logging.
        /// </summary>
        public void AddToLogs()
        {
            _log.Add("details", _details);
            _logs.Add(_log);
            _log = new Dictionary<string, object>();
            _details = new Dictionary<string, object>();
        }

        /// <summary>
        /// Serializes the _logs to be exported as a JSON file. 
        /// </summary>
        /// <returns></returns>
        public string ToJson() => JsonSerializer.Serialize(_logs);
    }
}