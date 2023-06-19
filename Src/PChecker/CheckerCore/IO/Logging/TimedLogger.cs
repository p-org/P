using System.IO;
using PChecker.Actors;
using PChecker.Actors.EventQueues.Mocks;
using PChecker.Actors.Events;

namespace PChecker.IO.Logging
{
    /// <summary>
    /// Logger that writes latencies.
    /// </summary>
    public sealed class TimedLogger
    {
        /// <summary>
        /// Underlying thread-safe in-memory logger.
        /// </summary>
        private InMemoryLogger InMemoryLogger;

        /// <summary>
        /// Log file path.
        /// </summary>
        private string LogFilePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimedLogger"/> class.
        /// </summary>
        /// /// <param name="checkerConfiguration">Checker configuration.</param>
        public TimedLogger(CheckerConfiguration checkerConfiguration)
        {
            Directory.CreateDirectory(checkerConfiguration.OutputFilePath + "/TimedLogs");
            LogFilePath = checkerConfiguration.OutputFilePath + "/TimedLogs/Log" + checkerConfiguration.CurrentIteration + ".csv";
            InMemoryLogger = new InMemoryLogger();
            InMemoryLogger.WriteLine("Time,Operation,Event,Source,State,Target");
        }

        /// <summary>
        /// Logs that the specified event is sent to a target actor at current time.
        /// </summary>
        /// <param name="targetActorId">The id of the target actor.</param>
        /// <param name="senderName">The name of the sender, if any.</param>
        /// <param name="senderState">The state name, if the sender is a state machine, else null.</param>
        /// <param name="e">The event being sent.</param>
        public void OnSendEvent(ActorId targetActorId, string senderName, string senderState, Event e)
        {
            InMemoryLogger.WriteLine(MockEventQueue.GetTime() + ", Send, " + e + ", " + senderName + ", " + senderState + ", " + targetActorId);
        }

        /// <summary>
        /// Logs that the specified event is dequeued by an actor at current time.
        /// </summary>
        /// <param name="id">The id of the actor that the event is being dequeued by.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="e">The event being dequeued.</param>
        public void OnDequeueEvent(ActorId id, string stateName, Event e)
        {
            InMemoryLogger.WriteLine(MockEventQueue.GetTime() + ", Dequeue, " + e + ", " + id + ", " + stateName + ", null");
        }

        /// <summary>
        /// Use this method to write log to the file.
        /// </summary>
        public void OnCompleted()
        {
            InMemoryLogger.WriteLine(MockEventQueue.GetTime() + ", Completed, null, null, null, null");
            File.WriteAllText(LogFilePath, InMemoryLogger.ToString());
            InMemoryLogger.Dispose();
        }
    }
}
