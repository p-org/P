// Copyright (c) 2010 Microsoft Corporation.  All rights reserved.
//
//
// Use of this source code is subject to the terms of the Microsoft
// license agreement under which you licensed this source code.
// If you did not accept the terms of the license agreement,
// you are not authorized to use this source code.
// For the terms of the license, please see the license agreement
// signed by you and Microsoft.
// THE SOURCE CODE IS PROVIDED "AS IS", WITH NO WARRANTIES OR INDEMNITIES.
//
using System;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

namespace ReviewFailedTests.Storage
{

    /// <summary>
    /// Isolated storage file helper class
    /// </summary>
    /// <typeparam name="T">Data type to serialize/deserialize</typeparam>
    public class IsolatedStorage<T>
    {
        private string GetAppDataPath()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft\\ReviewFailedTests");
            Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// Loads data from a file
        /// </summary>
        /// <param name="fileName">Name of the file to read.</param>
        /// <returns>Data object</returns>
        public T LoadFromFile(string fileName)
        {
            T loadedFile = default(T);
            string file = Path.Combine(GetAppDataPath(), fileName);
            if (File.Exists(file))
            {
                using (var myFileStream = new FileStream(file, FileMode.Open))
                {
                    // Call the Deserialize method and cast to the object type.
                    XmlSerializer mySerializer = new XmlSerializer(typeof(T));
                    loadedFile = (T)mySerializer.Deserialize(myFileStream);
                }
            }
            return loadedFile;
        }

        public T LoadFromStream(Stream s)
        {
            // Call the Deserialize method and cast to the object type.
            XmlSerializer mySerializer = new XmlSerializer(typeof(T));
            return (T)mySerializer.Deserialize(s);
        }

        /// <summary>
        /// Saves data to a file.
        /// </summary>
        /// <param name="fileName">Name of the file to write to</param>
        /// <param name="data">The data to save</param>
        public void SaveToFile(string fileName, T data)
        {
            string file = Path.Combine(GetAppDataPath(), fileName);
            XmlSerializer mySerializer = new XmlSerializer(typeof(T));
            using (StreamWriter myWriter = new StreamWriter(file, false, Encoding.UTF8))
            {
                mySerializer.Serialize(myWriter, data);
            }
        }

    }

}