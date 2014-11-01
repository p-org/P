//-----------------------------------------------------------------------
// <copyright file="Profiler.cs">
//      Copyright (c) 2014 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics;

namespace Microsoft.PSharp
{
    internal static class Profiler
    {
        private static Stopwatch StopWatch = null;

        /// <summary>
        /// Starts measuring execution time.
        /// </summary>
        public static void StartMeasuringExecutionTime()
        {
            Profiler.StopWatch = new Stopwatch();
            Profiler.StopWatch.Start();
        }

        /// <summary>
        /// Stops measuring execution time.
        /// </summary>
        public static void StopMeasuringExecutionTime()
        {
            Profiler.StopWatch.Stop();
        }

        /// <summary>
        /// Prints profiling results.
        /// </summary>
        public static void PrintResults()
        {
            Console.Error.WriteLine("Total Runtime: " + Profiler.StopWatch.Elapsed.TotalSeconds + " (sec).");
        }
    }
}
