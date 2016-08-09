using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace P.PRuntime
{
    /// <summary>
    /// This is the base class for all Prt exceptions.
    /// </summary>
    [Serializable]
    public abstract class PrtException : Exception
    {
        protected PrtException()
            : base()
        {
        }

        protected PrtException(string message)
            : base(message)
        {
        }

        protected virtual string ExceptionMessage { get { return string.Empty; } }

        /// <summary>
        /// Returns a formatted version of the exception for human consumption.
        /// </summary>
        /// <returns>String-formatted version of the exception</returns>
        public sealed override string ToString()
        {
            string exceptionMessage = this.ExceptionMessage;

            if (exceptionMessage != null && exceptionMessage.Length > 0)
                return string.Format(CultureInfo.CurrentUICulture,
                    "{0}\r\n{1}\r\n", this.ExceptionMessage, StackTrace);
            else
                return base.ToString();
        }

        /// <summary>
        /// Returns a Prt state backtrace from the point at which the exception
        /// was thrown, if possible.
        /// </summary>
        /// <remarks>
        /// This needs to be fixed with respect to P
        /// </remarks>
        private string stackTrace = null;

        public override string StackTrace
        {
            get
            {
                if (stackTrace == null)
                {
                    stackTrace = BuildStackTrace();
                }
                return (stackTrace);
            }
        }

        /// <summary>
        /// Useful in the case of multithreaded execution of Zinger
        /// </summary>
        public int myThreadId;

        private string BuildStackTrace()
        {

            return "NOT IMPLEMENTED";
            //
            // We encounter many exceptions during state-space exploration, so
            // we only want to build a stack trace when we're preparing a Zing
            // trace for viewing. Options.ExecuteTraceStatements is the best way to tell
            // if that is the case.
            //
            /*
            PSourceContext SourceContext;
            bool IsInnerMostFrame = true;
            if (ZingerConfiguration.ExecuteTraceStatements && Process.LastProcess[myThreadId] != null &&
                Process.LastProcess[myThreadId].TopOfStack != null)
            {
                StateImpl s = Process.LastProcess[myThreadId].StateImpl;
                StringBuilder sb = new StringBuilder();
                string[] sourceFiles = s.GetSourceFiles();

                sb.AppendFormat(CultureInfo.CurrentUICulture, "\r\nStack trace:\r\n");

                for (ZingMethod sf = Process.LastProcess[myThreadId].TopOfStack; sf != null; sf = sf.Caller)
                {
                    if (this is PrtAssertFailureException && IsInnerMostFrame)
                    {
                        SourceContext = Process.AssertionFailureCtx[myThreadId];
                        IsInnerMostFrame = false;
                    }
                    else
                    {
                        SourceContext = sf.Context;
                    }
                    string[] sources = s.GetSources();

                    if (sources != null && SourceContext.DocIndex < sources.Length)
                    {
                        // Translate the column offset to a line offset. This is horribly
                        // inefficient since we don't cache anything, but we do this rarely
                        // so the added complexity isn't worthwhile.
                        string sourceText = sources[SourceContext.DocIndex];
                        string[] sourceLines = sourceText.Split('\n');

                        int line, offset;
                        for (line = 0, offset = 0; offset < SourceContext.StartColumn && line < sourceLines.Length; line++)
                            offset += sourceLines[line].Length + 1;

                        sb.AppendFormat(CultureInfo.CurrentUICulture, "    {0} ({1}, Line {2})\r\n",
                            Utils.Unmangle(sf.MethodName),
                            System.IO.Path.GetFileName(sourceFiles[SourceContext.DocIndex]),
                            line);
                    }
                    else
                    {
                        // If we don't have Zing source, just provide the method names
                        sb.AppendFormat(CultureInfo.CurrentUICulture, "    {0}\r\n",
                            Utils.Unmangle(sf.MethodName));
                    }
                }

                return sb.ToString();
            }
            else
                return string.Empty;
          */
        }

    }

    public class PrtIllegalEnqueueException : PrtException
    {
        public PrtIllegalEnqueueException()
            : base()
        {
        }

        public PrtIllegalEnqueueException(string message)
            : base(message)
        {
        }

    }

    public class PrtInhabitsTypeException : PrtException
    {
        public PrtInhabitsTypeException()
            : base()
        {
        }

        public PrtInhabitsTypeException(string message)
            : base(message)
        {
        }
    }

    public class PrtMaxBufferSizeExceededException : PrtException
    {
        public PrtMaxBufferSizeExceededException()
            : base()
        {
        }

        public PrtMaxBufferSizeExceededException(string message)
            : base(message)
        {
        }
    }

    public class PrtInternalException : PrtException
    {
        public PrtInternalException()
            : base()
        {
        }

        public PrtInternalException(string message)
            : base(message)
        {
        }
    }

    public class PrtDeadlockException : PrtException
    {
        public PrtDeadlockException()
            : base()
        {
        }

        public PrtDeadlockException(string message)
            : base(message)
        {
        }

    }

    public class PrtUnhandledEventException : PrtException
    {
        public PrtUnhandledEventException()
            : base()
        {
        }

        public PrtUnhandledEventException(string message)
            : base(message)
        {
        }

    }

    public class PrtApplicationException : PrtException
    {
        public PrtApplicationException()
            : base()
        {
        }

        public PrtApplicationException(string message)
            : base(message)
        {
        }
    }

    public class PrtAssertFailureException : PrtException
    {
        public PrtAssertFailureException()
            : base()
        {
        }

        public PrtAssertFailureException(string message)
            : base(message)
        {
        }

    }

    public class PrtMaxEventInstancesExceededException : PrtException
    {
        public PrtMaxEventInstancesExceededException()
            : base()
        {
        }

        public PrtMaxEventInstancesExceededException(string message)
            : base(message)
        {
        }
    }

    public class PrtAssumeFailureException : PrtException
    {
        public PrtAssumeFailureException()
            : base()
        {
        }

        public PrtAssumeFailureException(string message)
            : base(message)
        {
        }
    }
}