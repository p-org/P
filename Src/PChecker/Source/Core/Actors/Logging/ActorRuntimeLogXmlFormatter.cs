// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Coyote.Actors.Timers;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// This class implements IActorRuntimeLog and generates log output in an XML format.
    /// </summary>
    internal class ActorRuntimeLogXmlFormatter : IActorRuntimeLog
    {
        private readonly XmlWriter Writer;
        private bool Closed;

        public ActorRuntimeLogXmlFormatter(XmlWriter writer)
        {
            this.Writer = writer;
            this.Writer.WriteStartElement("Log");
        }

        /// <summary>
        /// Invoked when a log is complete (and is about to be closed).
        /// </summary>
        public void OnCompleted()
        {
            this.Closed = true;
            using (this.Writer)
            {
                this.Writer.WriteEndElement();
            }
        }

        public void OnAssertionFailure(string error)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteElementString("AssertionFailure", error);
        }

        public void OnCreateActor(ActorId id, string creatorName, string creatorType)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteStartElement("CreateActor");
            this.Writer.WriteAttributeString("id", id.ToString());

            if (creatorName != null && creatorType != null)
            {
                this.Writer.WriteAttributeString("creatorName", creatorName);
                this.Writer.WriteAttributeString("creatorType", creatorType);
            }
            else
            {
                this.Writer.WriteAttributeString("creatorName", Task.CurrentId.ToString());
                this.Writer.WriteAttributeString("creatorType", "task");
            }

            this.Writer.WriteEndElement();
        }

        /// <inheritdoc/>
        public void OnCreateStateMachine(ActorId id, string creatorName, string creatorType)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteStartElement("CreateStateMachine");
            this.Writer.WriteAttributeString("id", id.ToString());

            if (creatorName != null && creatorType != null)
            {
                this.Writer.WriteAttributeString("creatorName", creatorName);
                this.Writer.WriteAttributeString("creatorType", creatorType);
            }
            else
            {
                this.Writer.WriteAttributeString("creatorName", Task.CurrentId.ToString());
                this.Writer.WriteAttributeString("creatorType", "task");
            }

            this.Writer.WriteEndElement();
        }

        public void OnCreateTimer(TimerInfo info)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteStartElement("CreateTimer");
            this.Writer.WriteAttributeString("owner", info.OwnerId?.Name ?? Task.CurrentId.ToString());
            this.Writer.WriteAttributeString("due", info.DueTime.ToString());
            this.Writer.WriteAttributeString("period", info.Period.ToString());
            this.Writer.WriteEndElement();
        }

        public void OnDefaultEventHandler(ActorId id, string stateName)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteStartElement("DefaultEvent");
            this.Writer.WriteAttributeString("id", id.ToString());
            if (!string.IsNullOrEmpty(stateName))
            {
                this.Writer.WriteAttributeString("state", stateName);
            }

            this.Writer.WriteEndElement();
        }

        public void OnDequeueEvent(ActorId id, string stateName, Event e)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteStartElement("DequeueEvent");
            this.Writer.WriteAttributeString("id", id.ToString());
            if (!string.IsNullOrEmpty(stateName))
            {
                this.Writer.WriteAttributeString("state", stateName);
            }

            this.Writer.WriteAttributeString("event", e.GetType().FullName);
            this.Writer.WriteEndElement();
        }

        public void OnEnqueueEvent(ActorId id, Event e)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteStartElement("EnqueueEvent");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("event", e.GetType().FullName);
            this.Writer.WriteEndElement();
        }

        public void OnExceptionHandled(ActorId id, string stateName, string actionName, Exception ex)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteStartElement("ExceptionHandled");
            this.Writer.WriteAttributeString("id", id.ToString());
            if (!string.IsNullOrEmpty(stateName))
            {
                this.Writer.WriteAttributeString("state", stateName);
            }

            this.Writer.WriteAttributeString("action", actionName);
            this.Writer.WriteAttributeString("type", ex.GetType().FullName);
            this.Writer.WriteString(ex.ToString());
            this.Writer.WriteEndElement();
        }

        public void OnExceptionThrown(ActorId id, string stateName, string actionName, Exception ex)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteStartElement("ExceptionThrown");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("state", stateName);
            this.Writer.WriteAttributeString("action", actionName);
            this.Writer.WriteAttributeString("type", ex.GetType().FullName);
            this.Writer.WriteString(ex.ToString());
            this.Writer.WriteEndElement();
        }

        public void OnExecuteAction(ActorId id, string handlingStateName, string currentStateName, string actionName)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteStartElement("Action");
            this.Writer.WriteAttributeString("id", id.ToString());
            if (!string.IsNullOrEmpty(currentStateName))
            {
                this.Writer.WriteAttributeString("state", currentStateName);
                if (currentStateName != handlingStateName)
                {
                    this.Writer.WriteAttributeString("handledBy", handlingStateName);
                }
            }

            this.Writer.WriteAttributeString("action", actionName);
            this.Writer.WriteEndElement();
        }

        public void OnGotoState(ActorId id, string currentStateName, string newStateName)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteStartElement("Goto");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("currState", currentStateName);
            this.Writer.WriteAttributeString("newState", newStateName);
            this.Writer.WriteEndElement();
        }

        public void OnHalt(ActorId id, int inboxSize)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteStartElement("Halt");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("inboxSize", inboxSize.ToString());
            this.Writer.WriteEndElement();
        }

        public void OnHandleRaisedEvent(ActorId id, string stateName, Event e)
        {
            if (this.Closed)
            {
                return;
            }
        }

        public void OnPopState(ActorId id, string currentStateName, string restoredStateName)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteStartElement("Pop");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("currState", currentStateName);
            this.Writer.WriteAttributeString("restoredState", restoredStateName);
            this.Writer.WriteEndElement();
        }

        public void OnPopStateUnhandledEvent(ActorId id, string stateName, Event e)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteStartElement("PopUnhandled");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("state", stateName);
            this.Writer.WriteAttributeString("event", e.GetType().FullName);
            this.Writer.WriteEndElement();
        }

        public void OnPushState(ActorId id, string currentStateName, string newStateName)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteStartElement("Push");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("currState", currentStateName);
            this.Writer.WriteAttributeString("newState", newStateName);
            this.Writer.WriteEndElement();
        }

        public void OnRaiseEvent(ActorId id, string stateName, Event e)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteStartElement("Raise");
            this.Writer.WriteAttributeString("id", id.ToString());
            if (!string.IsNullOrEmpty(stateName))
            {
                this.Writer.WriteAttributeString("state", stateName);
            }

            this.Writer.WriteAttributeString("event", e.GetType().FullName);
            this.Writer.WriteEndElement();
        }

        public void OnReceiveEvent(ActorId id, string stateName, Event e, bool wasBlocked)
        {
            if (this.Closed)
            {
                return;
            }

            var eventName = e.GetType().FullName;
            this.Writer.WriteStartElement("Receive");
            this.Writer.WriteAttributeString("id", id.ToString());
            if (!string.IsNullOrEmpty(stateName))
            {
                this.Writer.WriteAttributeString("state", stateName);
            }

            this.Writer.WriteAttributeString("event", eventName);
            this.Writer.WriteAttributeString("wasBlocked", wasBlocked.ToString());
            this.Writer.WriteEndElement();
        }

        public void OnSendEvent(ActorId targetActorId, string senderName, string senderType, string senderStateName,
            Event e, Guid opGroupId, bool isTargetHalted)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteStartElement("Send");
            this.Writer.WriteAttributeString("target", targetActorId.ToString());

            if (senderName != null && senderType != null)
            {
                this.Writer.WriteAttributeString("senderName", senderName);
                this.Writer.WriteAttributeString("senderType", senderType);
            }

            // TODO: should this be guarded as above?
            this.Writer.WriteAttributeString("senderState", senderStateName);

            this.Writer.WriteAttributeString("event", e.GetType().FullName);
            if (opGroupId != Guid.Empty)
            {
                this.Writer.WriteAttributeString("event", opGroupId.ToString());
            }

            this.Writer.WriteAttributeString("isTargetHalted", isTargetHalted.ToString());
            this.Writer.WriteEndElement();
        }

        public void OnStateTransition(ActorId id, string stateName, bool isEntry)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteStartElement("State");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("state", stateName);
            this.Writer.WriteAttributeString("isEntry", isEntry.ToString());
            this.Writer.WriteEndElement();
        }

        public void OnStopTimer(TimerInfo info)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteStartElement("StopTimer");
            this.Writer.WriteAttributeString("owner", info.OwnerId?.Name ?? Task.CurrentId.ToString());
            this.Writer.WriteEndElement();
        }

        public void OnWaitEvent(ActorId id, string stateName, Type eventType)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteStartElement("WaitEvent");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("event", eventType.FullName);
            if (!string.IsNullOrEmpty(stateName))
            {
                this.Writer.WriteAttributeString("state", stateName);
            }

            this.Writer.WriteEndElement();
        }

        public void OnWaitEvent(ActorId id, string stateName, params Type[] eventTypes)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteStartElement("WaitEvent");
            this.Writer.WriteAttributeString("id", id.ToString());
            StringBuilder sb = new StringBuilder();
            foreach (var t in eventTypes)
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(t.FullName);
            }

            this.Writer.WriteAttributeString("event", sb.ToString());
            if (!string.IsNullOrEmpty(stateName))
            {
                this.Writer.WriteAttributeString("state", stateName);
            }

            this.Writer.WriteEndElement();
        }

        public void OnCreateMonitor(string monitorType)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteStartElement("CreateMonitor");
            this.Writer.WriteAttributeString("type", monitorType);
            this.Writer.WriteEndElement();
        }

        public void OnMonitorExecuteAction(string monitorType, string stateName, string actionName)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteStartElement("MonitorAction");
            this.Writer.WriteAttributeString("monitorType", monitorType);
            this.Writer.WriteAttributeString("state", stateName);
            this.Writer.WriteAttributeString("action", actionName);
            this.Writer.WriteEndElement();
        }

        public void OnMonitorProcessEvent(string monitorType, string stateName, string senderName,
            string senderType, string senderStateName, Event e)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteStartElement("MonitorEvent");
            this.Writer.WriteAttributeString("monitorType", monitorType);
            this.Writer.WriteAttributeString("state", stateName);

            if (senderName != null && senderType != null)
            {
                this.Writer.WriteAttributeString("senderName", senderName);
                this.Writer.WriteAttributeString("senderType", senderType);
            }
            else
            {
                this.Writer.WriteAttributeString("senderName", Task.CurrentId.ToString());
                this.Writer.WriteAttributeString("senderType", "task");
            }

            // TODO: should this be guarded as above?
            this.Writer.WriteAttributeString("senderState", senderStateName);

            this.Writer.WriteAttributeString("event", e.GetType().FullName);
            this.Writer.WriteEndElement();
        }

        public void OnMonitorRaiseEvent(string monitorType, string stateName, Event e)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteStartElement("MonitorRaise");
            this.Writer.WriteAttributeString("monitorType", monitorType);
            this.Writer.WriteAttributeString("state", stateName);
            this.Writer.WriteAttributeString("event", e.GetType().FullName);
            this.Writer.WriteEndElement();
        }

        public void OnMonitorStateTransition(string monitorType, string stateName, bool isEntry, bool? isInHotState)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteStartElement("MonitorState");
            this.Writer.WriteAttributeString("monitorType", monitorType);
            this.Writer.WriteAttributeString("state", stateName);
            this.Writer.WriteAttributeString("isEntry", isEntry.ToString());
            bool hot = isInHotState == true;
            this.Writer.WriteAttributeString("isInHotState", hot.ToString());
            this.Writer.WriteEndElement();
        }

        public void OnMonitorError(string monitorType, string stateName, bool? isInHotState)
        {
        }

        public void OnRandom(object result, string callerName, string callerType)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteStartElement("Random");
            this.Writer.WriteAttributeString("result", result.ToString());

            if (callerName != null && callerType != null)
            {
                this.Writer.WriteAttributeString("creatorName", callerName);
                this.Writer.WriteAttributeString("creatorType", callerType);
            }
            else
            {
                this.Writer.WriteAttributeString("creatorName", Task.CurrentId.ToString());
                this.Writer.WriteAttributeString("creatorType", "task");
            }

            this.Writer.WriteEndElement();
        }

        public void OnStrategyDescription(string strategyName, string description)
        {
            if (this.Closed)
            {
                return;
            }

            this.Writer.WriteStartElement("Strategy");
            this.Writer.WriteAttributeString("strategy", strategyName);
            if (!string.IsNullOrEmpty(description))
            {
                this.Writer.WriteString(description);
            }

            this.Writer.WriteEndElement();
        }
    }
}
