#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PChecker.Actors.Logging
{
    /// <summary>
    /// Class for handling generating the vector clock for a log entry
    /// </summary>
    public class VectorClockGenerator
    {
        /// <summary>
        /// Nested class for handling FIFO send receive requests.
        /// NOTE: In the case of sending to a the same machine with the same event and same payload.
        /// </summary>
        private class FifoSendReceiveMapping
        {
            /// <summary>
            /// Private property for the number of counts of sends of the same machine, event, and payload.
            /// </summary>
            private int _sent { get; set; }

            /// <summary>
            /// Private property for the number of counts of receives of the same machine, event, and payload.
            /// </summary>
            private int _received { get; set; }

            /// <summary>
            /// Public getter and setter for _sent.
            /// </summary>
            public int SentCount
            {
                get => _sent;
                set => _sent = value;
            }

            /// <summary>
            /// Public getter and setter for _received.
            /// </summary>
            public int ReceivedCount
            {
                get => _received;
                set => _received = value;
            }

            /// <summary>
            /// Constructor for FifoSendReceiveMapping.
            /// Set _sent and _received to 0 initially.
            /// </summary>
            internal FifoSendReceiveMapping()
            {
                _sent = 0;
                _received = 0;
            }
        }

        /// <summary>
        /// Field declaration that keeps track of a global vector clock map of all the machines.
        /// </summary>
        public readonly Dictionary<string, Dictionary<string, int>> ContextVcMap;

        /// <summary>
        /// Field declaration that keeps track of unprocessed send requests. I.e., when a send request happened
        /// but hasn't had a corresponding receive event of it.
        /// </summary>
        private readonly Dictionary<string, Dictionary<string, int>> _unhandledSendRequests;

        /// <summary>
        /// Hash set to keep track of all unique machines.
        /// </summary>
        private readonly HashSet<string> _machines;

        /// <summary>
        /// Field declaration to keep track of send requests with the same target machine, event name, and payload.
        /// </summary>
        private readonly Dictionary<string, FifoSendReceiveMapping> _sendRequestsCount;

        /// <summary>
        /// VectorClockGenerator constructor
        /// Initialize empty mappings for _contextVcMap, _unhandledSendRequests, _machines, and _sendRequestsCount.
        /// </summary>
        public VectorClockGenerator()
        {
            ContextVcMap = new Dictionary<string, Dictionary<string, int>>();
            _unhandledSendRequests = new Dictionary<string, Dictionary<string, int>>();
            _machines = new HashSet<string>();
            _sendRequestsCount = new Dictionary<string, FifoSendReceiveMapping>();
        }

        /// <summary>
        /// Checks to see if machine name is new.
        /// </summary>
        /// <param name="machine">Machine name</param>
        /// <returns>bool: true when machine is new, false otherwise.</returns>
        private bool MachineIsNew(string machine) => !_machines.Contains(machine);

        /// <summary>
        /// Get the machine name for appropriate naming of the vector clock map.
        /// </summary>
        /// <param name="logDetails">Of type LogDetails: only names can be extracted from Id, Monitor, or Sender</param>
        /// <returns>string: the name of the machine for the type among the possible attributes containing the machine name.</returns>
        private static string GetMachineName(LogDetails logDetails) =>
            (logDetails.Id ?? logDetails.Monitor ?? logDetails.Sender)!;

        /// <summary>
        /// Hashes a string.
        /// </summary>
        /// <param name="input">Some string input.</param>
        /// <returns>string: the hashed string result.</returns>
        private static string HashString(string input)
        {
            using var sha256 = SHA256.Create();
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = sha256.ComputeHash(inputBytes);
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Makes a copy of the vector clock map.
        /// </summary>
        /// <param name="vcMap">Of type Dictionary&lt;string, int&gt;: the original vector clock map needed to be copied.</param>
        /// <returns>Dictionary&lt;string, int&gt;: the copied vector clock map.</returns>
        private static Dictionary<string, int> CopyVcMap(Dictionary<string, int> vcMap) =>
            vcMap.ToDictionary(entry => entry.Key, entry => entry.Value);

        /// <summary>
        /// Converts payload from json to string representation.
        ///
        /// Recursively builds string in case of nested data types.
        /// </summary>
        /// <param name="eventPayload">Of type object: JSON representation of payload.</param>
        /// <returns>string: string representation of payload.</returns>
        private static string? ConvertPayloadToString(object? eventPayload)
        {
            switch (eventPayload)
            {
                // If no payload, return empty string
                case null:
                    return string.Empty;

                // If payload is of Dictionary, iterate through key, value pair and build string. Recurse on the value
                // in the case that the value is another dictionary/list rather than a primitive type
                case IDictionary eventPayloadDict:
                {
                    var stringBuilder = new StringBuilder();
                    var eventPayloadDictKeys = eventPayloadDict.Keys;
                    foreach (var key in eventPayloadDictKeys)
                    {
                        stringBuilder.Append($"{key}: {ConvertPayloadToString(eventPayloadDict[key])}, ");
                    }

                    // Remove the last ", "
                    if (stringBuilder.Length >= 2)
                    {
                        stringBuilder.Length -= 2;
                    }

                    // Surround string with { and }
                    return $"{{ {stringBuilder} }}";
                }

                // If payload is of List, iterate through each item, and build string. Recurse on the item value
                // in the case that the value is another dictionary/list rather than a primitive type
                case IList eventPayloadList:
                {
                    var stringBuilder = new StringBuilder();
                    foreach (var value in eventPayloadList)
                    {
                        stringBuilder.Append($"{ConvertPayloadToString(value)}, ");
                    }

                    // Remove the last ", "
                    if (stringBuilder.Length >= 2)
                    {
                        stringBuilder.Length -= 2;
                    }

                    // Surround string with [ and ]
                    return $"[ {stringBuilder} ]";
                }

                // Just convert primitive types to string
                default:
                    return eventPayload.ToString();
            }
        }

        /// <summary>
        /// Gets the unique string to be hashed that contain information regarding a send/receive event.
        /// In the case of a send, machineName will be the target machine that's to be sent to.
        /// In the case of a receive, machineName will simply be the machine that receives it.
        /// </summary>
        /// <param name="machineName">of type string: name of the machine.</param>
        /// <param name="eventName">Of type string: name of the event.</param>
        /// <param name="eventPayload">Of type object: payload of the event, if there is any.</param>
        /// <returns>string: the string containing all information.</returns>
        private static string GetSendReceiveId(string? machineName, string? eventName, object? eventPayload) =>
            $"_{machineName}:_{eventName}:_{ConvertPayloadToString(eventPayload)}";

        private void updateMachineVcMap(string machine, Dictionary<string, int> senderVcMap)
        {
            // Get a set of all machine names to update between the sender vc map and the current machine vc map (minus the current machine)
            var machinesToUpdateInVc =
                new HashSet<string>(ContextVcMap[machine].Keys.Union(senderVcMap.Keys).Except(new[] { machine }));

            // Update local machine's vector clock in _contextVcMap, outside of itself, since it was already updated (incremented) from above
            // right before the switch case.
            // The rule for the remaining machines to be updated is taking the max between the sender machine's vector clock at that time and
            // the current machine's vector clock. Details can be found here: https://en.wikipedia.org/wiki/Vector_clock
            foreach (var machineToUpdate in machinesToUpdateInVc)
            {
                if (ContextVcMap[machine].TryGetValue(machineToUpdate, out var localMachineToUpdateValue))
                {
                    if (senderVcMap.TryGetValue(machineToUpdate, out var senderMachineToUpdateValue))
                    {
                        ContextVcMap[machine][machineToUpdate] =
                            Math.Max(senderMachineToUpdateValue, localMachineToUpdateValue);
                    }
                }
                else
                {
                    ContextVcMap[machine].Add(machineToUpdate, senderVcMap[machineToUpdate]);
                }
            }
        }

        /// <summary>
        /// Main method to update the vector clock mappings.
        /// </summary>
        /// <param name="logEntry">Of type LogEntry: the log entry information. Directly modifies the logEntry with the vector clock</param>
        public void HandleLogEntry(LogEntry logEntry)
        {
            // Get the type, and details of the log and get the machine name associated with it.
            var logType = logEntry.Type;
            var logDetails = logEntry.Details;
            var machine = GetMachineName(logDetails);

            // If new machine, create a new mapping for it in the _contextVcMap and add it to _machines.
            if (MachineIsNew(machine))
            {
                _machines.Add(machine);
                ContextVcMap.Add(machine, new Dictionary<string, int> { { machine, 0 } });
            }

            // Always update the local machine count by one on any event.
            ContextVcMap[machine][machine] += 1;

            switch (logType)
            {
                // On send event, hash the string containing information about the target machine to be sent to, the event name, and payload
                // This is used as an unique id necessary to correctly update the vector clock for the right machine-event-payload combo that
                // receives it later.
                case "SendEvent":
                    var targetMachine = logDetails.Target;
                    var sendReqId = GetSendReceiveId(targetMachine, logDetails.Event, logDetails.Payload);
                    var hashedGeneralSendReqId = HashString(sendReqId);

                    // If we have seen this sendReqId before, increment the sent count of it in _sendRequestsCount
                    if (_sendRequestsCount.TryGetValue(hashedGeneralSendReqId, out var generalSendReqIdValue))
                    {
                        generalSendReqIdValue.SentCount += 1;
                    }
                    // Else, create an instance of FifoSendReceiveMapping corresponding to the sendReqId in _sendRequestsCount.
                    else
                    {
                        _sendRequestsCount.Add(hashedGeneralSendReqId, new FifoSendReceiveMapping());
                    }

                    // Update the sendReqId with the send count of it.
                    sendReqId += $":_{_sendRequestsCount[hashedGeneralSendReqId].SentCount}";
                    var hashedSendReqId = HashString(sendReqId);
                    _unhandledSendRequests.Add(hashedSendReqId, CopyVcMap(ContextVcMap[machine]));
                    break;

                // For MonitorProcessEvents, tie it to the senderMachine's current vector clock
                // so that there is some association in the timeline
                case "MonitorProcessEvent":
                    if (logDetails.Sender != null) updateMachineVcMap(machine, ContextVcMap[logDetails.Sender]);
                    break;

                // On dequeue OR receive event, has the string containing information about the current machine that dequeued (i.e. received the event),
                // the event name, and payload. This is used to find the corresponding SendReqId from the machine that sent it in order to retrieve
                // the vector clock of the sender machine during that time when it was sent.
                case "ReceiveEvent":
                case "DequeueEvent":
                    var correspondingSendReqId = GetSendReceiveId(machine, logDetails.Event, logDetails.Payload);
                    var hashedGeneralCorrespondingSendReqId = HashString(correspondingSendReqId);
                    var correspondingSendReqIdReceiveCount =
                        _sendRequestsCount[hashedGeneralCorrespondingSendReqId].ReceivedCount;

                    // If we have handled all sent requests (i.e. received count is the same as sent count for the general sendReqId),
                    // remove it from _sendRequestsCount.
                    if (correspondingSendReqIdReceiveCount ==
                        _sendRequestsCount[hashedGeneralCorrespondingSendReqId].SentCount)
                    {
                        _sendRequestsCount.Remove(hashedGeneralCorrespondingSendReqId);
                    }
                    // If not, increment received count.
                    else
                    {
                        _sendRequestsCount[hashedGeneralCorrespondingSendReqId].ReceivedCount += 1;
                    }

                    correspondingSendReqId += $":_{correspondingSendReqIdReceiveCount}";
                    var hashedCorrespondingSendReqId = HashString(correspondingSendReqId);
                    var senderVcMap = _unhandledSendRequests[hashedCorrespondingSendReqId];

                    updateMachineVcMap(machine, senderVcMap);

                    // Remove the SendReqId because we've processed it.
                    _unhandledSendRequests.Remove(hashedCorrespondingSendReqId);
                    break;
            }

            // Update the log entry with the vector clock.
            logEntry.Details.Clock = CopyVcMap(ContextVcMap[machine]);
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// The log type of the log entry.
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// The details of the log entry.
        /// </summary>
        public LogDetails Details { get; set; }

        /// <summary>
        /// LogEntry constructor.
        /// </summary>
        public LogEntry()
        {
            Details = new LogDetails();
        }
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
    public class LogDetails
    {
        /// <summary>
        /// The text log from PLogFormatter. Removes the log tags.
        /// I.e., no &lt;SomeLog&gt; in the beginning.
        /// Available for all log types.
        /// </summary>
        public string? Log { get; set; }

        /// <summary>
        /// The actor id.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// The error message if an error occurs.
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// The event name. I.e. eWithDrawReq, etc...
        /// </summary>
        public string? Event { get; set; }

        /// <summary>
        /// Name of creator.
        /// </summary>
        public string? CreatorName { get; set; }

        /// <summary>
        /// Type of creator.
        /// </summary>
        public string? CreatorType { get; set; }

        /// <summary>
        /// The state associated with an event, machine, etc...
        /// </summary>
        public string? State { get; set; }

        /// <summary>
        /// The initial state associated with an event, machine, etc...
        /// Available for log types GotoState, PopState, PushState
        /// </summary>
        public string? StartState { get; set; }

        /// <summary>
        /// The initial state associated with an event, machine, etc...
        /// Available for log types GotoState, PopState, PushState
        /// </summary>
        public string? EndState { get; set; }

        /// <summary>
        /// Payload of an event. Represented in object or primitive types, or Dictionary, or List.
        /// I.e.
        /// {
        ///     "source": "Client(4)",
        ///     "accountId": "0",
        ///     "amount": "52",
        ///     "rId": "1"
        /// }
        /// </summary>
        public object? Payload { get; set; }

        /// <summary>
        /// The action being executed.
        /// Available for log type ExceptionHandled and ExceptionThrown.
        /// </summary>
        public string? Action { get; set; }

        /// <summary>
        /// The name of the exception.
        /// Available for log type ExceptionHandled and ExceptionThrown.
        /// </summary>
        public string? Exception { get; set; }

        /// <summary>
        /// Approximate size of the inbox.
        /// Available for log type Halt.
        /// </summary>
        public int? HaltInboxSize { get; set; }

        /// <summary>
        /// Boolean representing whether an actor was waiting for one or more events
        /// Available for log type ReceiveEvent.
        /// </summary>
        public bool? WasBlocked { get; set; }

        /// <summary>
        /// Name of sender.
        /// Available for log type SendEvent.
        /// </summary>
        public string? Sender { get; set; }

        /// <summary>
        /// Id of target actor.
        /// Available for log type SendEvent.
        /// </summary>
        public string? Target { get; set; }

        /// <summary>
        /// The id for the send operation.
        /// Available for log type SendEvent.
        /// </summary>
        public string? OpGroupId { get; set; }

        /// <summary>
        /// Boolean representing whether the target actor was halted.
        /// Available for log type SendEvent.
        /// </summary>
        public bool? IsTargetHalted { get; set; }

        /// <summary>
        /// Boolean representing whether it's a state entry to exit.
        /// Available for log types StateTransition and MonitorStateTransition.
        /// </summary>
        public bool? IsEntry { get; set; }

        /// <summary>
        /// Boolean representing whether monitor is in hot or cold state.
        /// Available for log type MonitorStateTransition.
        /// </summary>
        public bool? IsInHotState { get; set; }

        /// <summary>
        /// The type of the event being waited for.
        /// Available for log type WaitEvent.
        /// </summary>
        public string? EventType { get; set; }

        /// <summary>
        /// The type of events being waited for.
        /// Available for log type WaitMultipleEvents.
        /// </summary>
        public List<string>? EventTypes { get; set; }

        /// <summary>
        /// Name of monitor.
        /// Available for log types CreateMonitor, MonitorProcessEvent,
        /// MonitorRaiseEvent, and MonitorStateTransition
        /// </summary>
        public string? Monitor { get; set; }

        /// <summary>
        /// Name of strategy used.
        /// Available for log type StrategyDescription.
        /// </summary>
        public string? Strategy { get; set; }

        /// <summary>
        /// Information about the scheduling strategy.
        /// Available for log type StrategyDescription.
        /// </summary>
        public string? StrategyDescription { get; set; }

        /// <summary>
        /// Vector map of the machine associated with the log entry
        /// </summary>
        public Dictionary<string, int>? Clock { get; set; }
    }

    /// <summary>
    /// JsonWriter to write the Json output.
    /// </summary>
    public class JsonWriter
    {
        /// <summary>
        /// List of LogEntry.
        /// </summary>
        private readonly List<LogEntry> _logs;

        /// <summary>
        /// LogEntry with fields Type and Details. Represents an individual log.
        /// </summary>
        private LogEntry _log;

        /// <summary>
        /// Vector clock generator instance to help with vector clock generation.
        /// </summary>
        internal VectorClockGenerator VcGenerator {get;}

        /// <summary>
        /// Getter for accessing log entry details.
        /// </summary>
        public LogDetails LogDetails => _log.Details;

        /// <summary>
        /// Getter for accessing logs.
        /// </summary>
        public List<LogEntry> Logs => _logs;

        /// <summary>
        /// Initializes the Writer. Create empty _logs, _log, and _details objects.
        /// </summary>
        public JsonWriter()
        {
            _logs = new List<LogEntry>();
            _log = new LogEntry();
            VcGenerator = new VectorClockGenerator();
        }

        /// <summary>
        /// Enum representing the different log types the JSON error trace logs.
        /// Referenced from PLogFormatter.cs and ActorRuntimeLogTextFormatter.cs
        /// to see what those formatter logs. Check IActorRuntimeLog.cs to see
        /// each log types' description and when they are invoked.
        /// </summary>
        public enum LogType
        {
            /// <summary>
            /// Invoked when the specified assertion failure has occurred.
            /// </summary>
            AssertionFailure,

            /// <summary>
            /// Invoked when the specified actor has been created.
            /// </summary>
            CreateActor,

            /// <summary>
            /// Invoked when the specified state machine has been created.
            /// </summary>
            CreateStateMachine,

            /// <summary>
            /// Invoked when the specified monitor has been created.
            /// </summary>
            CreateMonitor,

            /// <summary>
            /// Invoked when the specified actor is idle (there is nothing to dequeue) and the default
            /// event handler is about to be executed.
            /// </summary>
            DefaultEventHandler,

            /// <summary>
            /// Invoked when the specified event is dequeued by an actor.
            /// </summary>
            DequeueEvent,

            /// <summary>
            /// Invoked when the specified OnException method is used to handle a thrown exception.
            /// </summary>
            ExceptionHandled,

            /// <summary>
            /// Invoked when the specified actor throws an exception.
            /// </summary>
            ExceptionThrown,

            /// <summary>
            /// Invoked when the specified state machine performs a goto transition to the specified state.
            /// </summary>
            GotoState,

            /// <summary>
            /// Invoked when the specified actor has been halted.
            /// </summary>
            Halt,

            /// <summary>
            /// Invoked when the specified monitor is about to process an event.
            /// </summary>
            MonitorProcessEvent,

            /// <summary>
            /// Invoked when the specified monitor raised an event.
            /// </summary>
            MonitorRaiseEvent,

            /// <summary>
            /// Invoked when the specified monitor enters or exits a state.
            /// </summary>
            MonitorStateTransition,

            /// <summary>
            /// Invoked when the specified state machine has popped its current state.
            /// </summary>
            PopState,

            /// <summary>
            /// Invoked when the specified event cannot be handled in the current state, its exit
            /// handler is executed and then the state is popped and any previous "current state"
            /// is reentered. This handler is called when that pop has been done.
            /// </summary>
            PopStateUnhandledEvent,

            /// <summary>
            /// Invoked when the specified state machine is being pushed to a state.
            /// </summary>
            PushState,

            /// <summary>
            /// Invoked when the specified state machine raises an event.
            /// </summary>
            RaiseEvent,

            /// <summary>
            /// Invoked when the specified event is received by an actor.
            /// </summary>
            ReceiveEvent,

            /// <summary>
            /// Invoked when the specified event is sent to a target actor.
            /// </summary>
            SendEvent,

            /// <summary>
            /// Invoked when the specified state machine enters or exits a state.
            /// </summary>
            StateTransition,

            /// <summary>
            /// Invoked to describe the specified scheduling strategy.
            /// </summary>
            StrategyDescription,

            /// <summary>
            /// Invoked when the specified actor waits to receive an event of a specified type.
            /// </summary>
            WaitEvent,

            /// <summary>
            /// Invoked when the specified actor waits to receive multiple events of a specified type.
            /// </summary>
            WaitMultipleEvents,

            /// <summary>
            /// Invoked on a print statement
            /// </summary>
            Print,

            /// <summary>
            /// Invoked on a print statement
            /// </summary>
            Announce,
        }

        /// <summary>
        /// Adds log text to details dictionary.
        /// </summary>
        /// <param name="log">The log text.</param>
        public void AddLog(string log) => _log.Details.Log = log;


        /// <summary>
        /// Add the log type to _log dictionary.
        /// </summary>
        /// <param name="type">The log type</param>
        public void AddLogType(LogType type) => _log.Type = type.ToString();


        /// <summary>
        /// Add _log to _logs and resets the Writer for next available logging.
        /// </summary>
        /// <param name="updateVcMap">Of type Bool: If true, run HandleLogEntry to get vector clock. Else, don't.</param>
        public void AddToLogs(bool updateVcMap = false)
        {
            try
            {
                if (updateVcMap)
                {
                    VcGenerator.HandleLogEntry(_log);
                }

                _logs.Add(_log);
                _log = new LogEntry();
            }
            catch (Exception)
            {
                // ignoring exceptions in logger.
                // TODO: this needs to be fixed.
            }
        }
    }
}