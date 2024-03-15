// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using PChecker.Actors.Events;

namespace PChecker.Actors.Logging
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
            Writer = writer;
            Writer.WriteStartElement("Log");
        }

        /// <summary>
        /// Invoked when a log is complete (and is about to be closed).
        /// </summary>
        public void OnCompleted()
        {
            Closed = true;
            using (Writer)
            {
                Writer.WriteEndElement();
            }
        }

        public void OnAssertionFailure(string error)
        {
            if (Closed)
            {
                return;
            }

            Writer.WriteElementString("AssertionFailure", error);
        }

        public void OnCreateActor(ActorId id, string creatorName, string creatorType)
        {
            if (Closed)
            {
                return;
            }

            Writer.WriteStartElement("CreateActor");
            Writer.WriteAttributeString("id", id.ToString());

            if (creatorName != null && creatorType != null)
            {
                Writer.WriteAttributeString("creatorName", creatorName);
                Writer.WriteAttributeString("creatorType", creatorType);
            }
            else
            {
                Writer.WriteAttributeString("creatorName", Task.CurrentId.ToString());
                Writer.WriteAttributeString("creatorType", "task");
            }

            Writer.WriteEndElement();
        }

        /// <inheritdoc/>
        public void OnCreateStateMachine(ActorId id, string creatorName, string creatorType)
        {
            if (Closed)
            {
                return;
            }

            Writer.WriteStartElement("CreateStateMachine");
            Writer.WriteAttributeString("id", id.ToString());

            if (creatorName != null && creatorType != null)
            {
                Writer.WriteAttributeString("creatorName", creatorName);
                Writer.WriteAttributeString("creatorType", creatorType);
            }
            else
            {
                Writer.WriteAttributeString("creatorName", Task.CurrentId.ToString());
                Writer.WriteAttributeString("creatorType", "task");
            }

            Writer.WriteEndElement();
        }

        public void OnDefaultEventHandler(ActorId id, string stateName)
        {
            if (Closed)
            {
                return;
            }

            Writer.WriteStartElement("DefaultEvent");
            Writer.WriteAttributeString("id", id.ToString());
            if (!string.IsNullOrEmpty(stateName))
            {
                Writer.WriteAttributeString("state", stateName);
            }

            Writer.WriteEndElement();
        }

        public void OnDequeueEvent(ActorId id, string stateName, Event e)
        {
            if (Closed)
            {
                return;
            }

            Writer.WriteStartElement("DequeueEvent");
            Writer.WriteAttributeString("id", id.ToString());
            if (!string.IsNullOrEmpty(stateName))
            {
                Writer.WriteAttributeString("state", stateName);
            }

            Writer.WriteAttributeString("event", e.GetType().FullName);
            Writer.WriteEndElement();
        }

        public void OnEnqueueEvent(ActorId id, Event e)
        {
            if (Closed)
            {
                return;
            }

            Writer.WriteStartElement("EnqueueEvent");
            Writer.WriteAttributeString("id", id.ToString());
            Writer.WriteAttributeString("event", e.GetType().FullName);
            Writer.WriteEndElement();
        }

        public void OnExceptionHandled(ActorId id, string stateName, string actionName, Exception ex)
        {
            if (Closed)
            {
                return;
            }

            Writer.WriteStartElement("ExceptionHandled");
            Writer.WriteAttributeString("id", id.ToString());
            if (!string.IsNullOrEmpty(stateName))
            {
                Writer.WriteAttributeString("state", stateName);
            }

            Writer.WriteAttributeString("action", actionName);
            Writer.WriteAttributeString("type", ex.GetType().FullName);
            Writer.WriteString(ex.ToString());
            Writer.WriteEndElement();
        }

        public void OnExceptionThrown(ActorId id, string stateName, string actionName, Exception ex)
        {
            if (Closed)
            {
                return;
            }

            Writer.WriteStartElement("ExceptionThrown");
            Writer.WriteAttributeString("id", id.ToString());
            Writer.WriteAttributeString("state", stateName);
            Writer.WriteAttributeString("action", actionName);
            Writer.WriteAttributeString("type", ex.GetType().FullName);
            Writer.WriteString(ex.ToString());
            Writer.WriteEndElement();
        }

        public void OnExecuteAction(ActorId id, string handlingStateName, string currentStateName, string actionName)
        {
            if (Closed)
            {
                return;
            }

            Writer.WriteStartElement("Action");
            Writer.WriteAttributeString("id", id.ToString());
            if (!string.IsNullOrEmpty(currentStateName))
            {
                Writer.WriteAttributeString("state", currentStateName);
                if (currentStateName != handlingStateName)
                {
                    Writer.WriteAttributeString("handledBy", handlingStateName);
                }
            }

            Writer.WriteAttributeString("action", actionName);
            Writer.WriteEndElement();
        }

        public void OnGotoState(ActorId id, string currentStateName, string newStateName)
        {
            if (Closed)
            {
                return;
            }

            Writer.WriteStartElement("Goto");
            Writer.WriteAttributeString("id", id.ToString());
            Writer.WriteAttributeString("currState", currentStateName);
            Writer.WriteAttributeString("newState", newStateName);
            Writer.WriteEndElement();
        }

        public void OnHalt(ActorId id, int inboxSize)
        {
            if (Closed)
            {
                return;
            }

            Writer.WriteStartElement("Halt");
            Writer.WriteAttributeString("id", id.ToString());
            Writer.WriteAttributeString("inboxSize", inboxSize.ToString());
            Writer.WriteEndElement();
        }

        public void OnHandleRaisedEvent(ActorId id, string stateName, Event e)
        {
            if (Closed)
            {
                return;
            }
        }

        public void OnPopState(ActorId id, string currentStateName, string restoredStateName)
        {
            if (Closed)
            {
                return;
            }

            Writer.WriteStartElement("Pop");
            Writer.WriteAttributeString("id", id.ToString());
            Writer.WriteAttributeString("currState", currentStateName);
            Writer.WriteAttributeString("restoredState", restoredStateName);
            Writer.WriteEndElement();
        }

        public void OnPopStateUnhandledEvent(ActorId id, string stateName, Event e)
        {
            if (Closed)
            {
                return;
            }

            Writer.WriteStartElement("PopUnhandled");
            Writer.WriteAttributeString("id", id.ToString());
            Writer.WriteAttributeString("state", stateName);
            Writer.WriteAttributeString("event", e.GetType().FullName);
            Writer.WriteEndElement();
        }

        public void OnPushState(ActorId id, string currentStateName, string newStateName)
        {
            if (Closed)
            {
                return;
            }

            Writer.WriteStartElement("Push");
            Writer.WriteAttributeString("id", id.ToString());
            Writer.WriteAttributeString("currState", currentStateName);
            Writer.WriteAttributeString("newState", newStateName);
            Writer.WriteEndElement();
        }

        public void OnRaiseEvent(ActorId id, string stateName, Event e)
        {
            if (Closed)
            {
                return;
            }

            Writer.WriteStartElement("Raise");
            Writer.WriteAttributeString("id", id.ToString());
            if (!string.IsNullOrEmpty(stateName))
            {
                Writer.WriteAttributeString("state", stateName);
            }

            Writer.WriteAttributeString("event", e.GetType().FullName);
            Writer.WriteEndElement();
        }

        public void OnReceiveEvent(ActorId id, string stateName, Event e, bool wasBlocked)
        {
            if (Closed)
            {
                return;
            }

            var eventName = e.GetType().FullName;
            Writer.WriteStartElement("Receive");
            Writer.WriteAttributeString("id", id.ToString());
            if (!string.IsNullOrEmpty(stateName))
            {
                Writer.WriteAttributeString("state", stateName);
            }

            Writer.WriteAttributeString("event", eventName);
            Writer.WriteAttributeString("wasBlocked", wasBlocked.ToString());
            Writer.WriteEndElement();
        }

        public void OnSendEvent(ActorId targetActorId, string senderName, string senderType, string senderStateName,
            Event e, Guid opGroupId, bool isTargetHalted)
        {
            if (Closed)
            {
                return;
            }

            Writer.WriteStartElement("Send");
            Writer.WriteAttributeString("target", targetActorId.ToString());

            if (senderName != null && senderType != null)
            {
                Writer.WriteAttributeString("senderName", senderName);
                Writer.WriteAttributeString("senderType", senderType);
            }

            // TODO: should this be guarded as above?
            Writer.WriteAttributeString("senderState", senderStateName);

            Writer.WriteAttributeString("event", e.GetType().FullName);
            if (opGroupId != Guid.Empty)
            {
                Writer.WriteAttributeString("event", opGroupId.ToString());
            }

            Writer.WriteAttributeString("isTargetHalted", isTargetHalted.ToString());
            Writer.WriteEndElement();
        }

        public void OnStateTransition(ActorId id, string stateName, bool isEntry)
        {
            if (Closed)
            {
                return;
            }

            Writer.WriteStartElement("State");
            Writer.WriteAttributeString("id", id.ToString());
            Writer.WriteAttributeString("state", stateName);
            Writer.WriteAttributeString("isEntry", isEntry.ToString());
            Writer.WriteEndElement();
        }

        public void OnWaitEvent(ActorId id, string stateName, Type eventType)
        {
            if (Closed)
            {
                return;
            }

            Writer.WriteStartElement("WaitEvent");
            Writer.WriteAttributeString("id", id.ToString());
            Writer.WriteAttributeString("event", eventType.FullName);
            if (!string.IsNullOrEmpty(stateName))
            {
                Writer.WriteAttributeString("state", stateName);
            }

            Writer.WriteEndElement();
        }

        public void OnWaitEvent(ActorId id, string stateName, params Type[] eventTypes)
        {
            if (Closed)
            {
                return;
            }

            Writer.WriteStartElement("WaitEvent");
            Writer.WriteAttributeString("id", id.ToString());
            var sb = new StringBuilder();
            foreach (var t in eventTypes)
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(t.FullName);
            }

            Writer.WriteAttributeString("event", sb.ToString());
            if (!string.IsNullOrEmpty(stateName))
            {
                Writer.WriteAttributeString("state", stateName);
            }

            Writer.WriteEndElement();
        }

        public void OnCreateMonitor(string monitorType)
        {
            if (Closed)
            {
                return;
            }

            Writer.WriteStartElement("CreateMonitor");
            Writer.WriteAttributeString("type", monitorType);
            Writer.WriteEndElement();
        }

        public void OnMonitorExecuteAction(string monitorType, string stateName, string actionName)
        {
            if (Closed)
            {
                return;
            }

            Writer.WriteStartElement("MonitorAction");
            Writer.WriteAttributeString("monitorType", monitorType);
            Writer.WriteAttributeString("state", stateName);
            Writer.WriteAttributeString("action", actionName);
            Writer.WriteEndElement();
        }

        public void OnMonitorProcessEvent(string monitorType, string stateName, string senderName,
            string senderType, string senderStateName, Event e)
        {
            if (Closed)
            {
                return;
            }

            Writer.WriteStartElement("MonitorEvent");
            Writer.WriteAttributeString("monitorType", monitorType);
            Writer.WriteAttributeString("state", stateName);

            if (senderName != null && senderType != null)
            {
                Writer.WriteAttributeString("senderName", senderName);
                Writer.WriteAttributeString("senderType", senderType);
            }
            else
            {
                Writer.WriteAttributeString("senderName", Task.CurrentId.ToString());
                Writer.WriteAttributeString("senderType", "task");
            }

            // TODO: should this be guarded as above?
            Writer.WriteAttributeString("senderState", senderStateName);

            Writer.WriteAttributeString("event", e.GetType().Name);
            Writer.WriteEndElement();
        }

        public void OnMonitorRaiseEvent(string monitorType, string stateName, Event e)
        {
            if (Closed)
            {
                return;
            }

            Writer.WriteStartElement("MonitorRaise");
            Writer.WriteAttributeString("monitorType", monitorType);
            Writer.WriteAttributeString("state", stateName);
            Writer.WriteAttributeString("event", e.GetType().FullName);
            Writer.WriteEndElement();
        }

        public void OnMonitorStateTransition(string monitorType, string stateName, bool isEntry, bool? isInHotState)
        {
            if (Closed)
            {
                return;
            }

            Writer.WriteStartElement("MonitorState");
            Writer.WriteAttributeString("monitorType", monitorType);
            Writer.WriteAttributeString("state", stateName);
            Writer.WriteAttributeString("isEntry", isEntry.ToString());
            var hot = isInHotState == true;
            Writer.WriteAttributeString("isInHotState", hot.ToString());
            Writer.WriteEndElement();
        }

        public void OnMonitorError(string monitorType, string stateName, bool? isInHotState)
        {
        }

        public void OnRandom(object result, string callerName, string callerType)
        {
            if (Closed)
            {
                return;
            }

            Writer.WriteStartElement("Random");
            Writer.WriteAttributeString("result", result.ToString());

            if (callerName != null && callerType != null)
            {
                Writer.WriteAttributeString("creatorName", callerName);
                Writer.WriteAttributeString("creatorType", callerType);
            }
            else
            {
                Writer.WriteAttributeString("creatorName", Task.CurrentId.ToString());
                Writer.WriteAttributeString("creatorType", "task");
            }

            Writer.WriteEndElement();
        }

        public void OnStrategyDescription(string strategyName, string description)
        {
            if (Closed)
            {
                return;
            }

            Writer.WriteStartElement("Strategy");
            Writer.WriteAttributeString("strategy", strategyName);
            if (!string.IsNullOrEmpty(description))
            {
                Writer.WriteString(description);
            }

            Writer.WriteEndElement();
        }
    }
}