//-----------------------------------------------------------------------
// <copyright file="Model.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;

using Microsoft.PSharp.Scheduling;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Static class implementing model specific methods.
    /// </summary>
    public static class Model
    {
        /// <summary>
        /// List containing the non deterministic boolean
        /// choices of the previous execution.
        /// </summary>
        private static List<bool> NDBooleanChoices = new List<bool>();

        /// <summary>
        /// Causes the model machine to sleep for the given
        /// amount of milliseconds.
        /// </summary>
        /// <param name="millisecondsTimeout"></param>
        public static void Sleep(int millisecondsTimeout)
        {
            Thread.Sleep(millisecondsTimeout);
        }

        /// <summary>
        /// Static class implementing nondeterministic values.
        /// </summary>
        public static class Havoc
        {
            /// <summary>
            /// Nondeterministic boolean value.
            /// </summary>
            public static bool Boolean
            {
                get
                {
                    return Havoc.GetBoolean();
                }
            }

            /// <summary>
            /// Nondeterministic integer value. The return
            /// value v is equal or greater than 0 and lower
            /// than the given ceiling.
            /// </summary>
            /// <param name="ceiling">Ceiling</param>
            /// <returns>int</returns>
            public static int Integer(int ceiling)
            {
                return Havoc.UnsignedInteger(ceiling);
            }

            /// <summary>
            /// Returns a nondeterministic boolean value.
            /// </summary>
            /// <returns>bool</returns>
            internal static bool GetBoolean()
            {
                bool result = false;

                if (Runtime.Options.Mode == Runtime.Mode.Replay)
                {
                    result = Model.NDBooleanChoices[0];
                    Model.NDBooleanChoices.RemoveAt(0);
                }
                else if (Runtime.Options.Mode == Runtime.Mode.BugFinding)
                {
                    if (Havoc.UnsignedInteger(2) == 1)
                        result = true;
                    Model.NDBooleanChoices.Add(result);
                }
                else
                {
                    if (Havoc.UnsignedInteger(2) == 1)
                        result = true;
                }

                return result;
            }

            /// <summary>
            /// Returns a non deterministic unsigned integer.
            /// The return value v is equal or greater than 0
            /// and lower than the given ceiling.
            /// </summary>
            /// <param name="ceiling">Ceiling</param>
            /// <returns>int</returns>
            internal static int UnsignedInteger(int ceiling)
            {
                RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

                byte[] buffer = new byte[4];
                int bc, val;

                if ((ceiling & -ceiling) == ceiling)
                {
                    rng.GetBytes(buffer);
                    bc = BitConverter.ToInt32(buffer, 0);
                    return bc & (ceiling - 1);
                }

                do
                {
                    rng.GetBytes(buffer);
                    bc = BitConverter.ToInt32(buffer, 0) & 0x7FFFFFFF;
                    val = bc % ceiling;
                } while (bc - val + (ceiling - 1) < 0);

                return val;
            }

            internal static List<int> List(int size)
            {
                List<int> result = new List<int>(size);
                HashSet<int> set = new HashSet<int>(result);

                for (int idx = 0; idx < size; idx++)
                {
                    int value;

                    do
                    {
                        value = Havoc.UnsignedInteger(size);
                    }
                    while (!set.Add(value));

                    result.Add(value);
                }

                return result;
            }
        }
    }
}
