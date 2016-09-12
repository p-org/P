//---------------------------------------------------------------------------
// File: PerfTimer.cs
//---------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Pc
{
    /// <summary>
    /// This is a high precision timer - useful for profiling compiler performance.
    /// </summary>
    internal class PerfTimer : IDisposable
    {
        Stopwatch watch;
        long m_Min;
        long m_Max;
        long m_Count;
        long m_Sum;
        string m_Caption;

        public static bool ConsoleOutput { get; set; }

#if __MonoCS__
        static void OutputDebugString(string lpOutputString) { }
#else
        [DllImport("Kernel32.dll")]
        static extern void OutputDebugString(string lpOutputString);
#endif

        public PerfTimer(string caption)
        {
            m_Caption = caption;
            watch = new Stopwatch();
            Start();
        }

        public void Start()
        {
            watch.Start();
        }

        public void Stop()
        {
            watch.Stop();
        }

        /// <summary>
        /// Get elapsed time in milliseconds
        /// </summary>
        public long GetDuration()
        {          
            return watch.ElapsedMilliseconds;
        }

        /// <summary>
        /// Get elapsed time in platform specific ticks
        /// </summary>
        /// <returns></returns>
        public long GetTicks()
        {
            return watch.ElapsedTicks;
        }

        /// <summary>
        /// Count up multiple iterations and then get the median, average and percent variation.
        /// </summary>
        /// <param name="ms"></param>
        public void Count(long ms)
        {
            if (m_Min == 0) m_Min = ms;
            if (ms < m_Min) m_Min = ms;
            if (ms > m_Max) m_Max = ms;
            m_Sum += ms;
            m_Count++;
        }

        public long Min()
        {
            return m_Min;
        }

        public long Max()
        {
            return m_Max;
        }

        public double Median()
        {
            return TwoDecimals(m_Min + ((m_Max - m_Min) / 2.0));
        }

        public double PercentError()
        {
            double spread = (m_Max - m_Min) / 2.0;
            double percent = TwoDecimals((double)(spread * 100.0) / (double)(m_Min));
            return percent;
        }

        static public double TwoDecimals(double i)
        {
            return Math.Round(i * 100) / 100;
        }

        public long Average()
        {
            if (m_Count == 0) return 0;
            return m_Sum / m_Count;
        }

        public void Clear()
        {
            watch.Reset();
            m_Min = m_Max = m_Sum = m_Count;
        }

        public void Dispose()
        {
            string msg = null;
            Stop();
            if (m_Count == 0)
            {
                msg = string.Format("{0} took {1} ms", m_Caption, GetDuration());
            }
            else
            {
                // print the average, min, max
                msg = string.Format("{0} with {1} iterations averaged {2} ms with min {3}, max {4}", m_Caption, m_Count, Average(), Min(), Max());
            }
            // we want this to work in Release build also, which is why we are not using Debug.WriteLine().
            OutputDebugString(msg);

            if (ConsoleOutput)
            {
                Console.WriteLine(msg);
            }
        }

        



    }
}