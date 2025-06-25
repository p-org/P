// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;

namespace PChecker.Coverage.Code
{
    /// <summary>
    /// Class for storing code coverage specific information about
    /// a single point of coverage in the code.
    /// </summary>
    [DataContract]
    public class CodeCoverageInfo : IEquatable<CodeCoverageInfo>
    {
        /// <summary>
        /// The user-provided label for this code point.
        /// </summary>
        [DataMember]
        public string Label { get; }

        /// <summary>
        /// The source code location information.
        /// </summary>
        [DataMember]
        public string CodeLocation { get; }

        /// <summary>
        /// The custom payload provided by the user.
        /// </summary>
        [DataMember]
        public string CustomPayload { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeCoverageInfo"/> class.
        /// </summary>
        /// <param name="label">User-provided label.</param>
        /// <param name="codeLocation">Code location identifier.</param>
        /// <param name="customPayload">Custom user-provided payload.</param>
        public CodeCoverageInfo(string label, string codeLocation, string customPayload)
        {
            Label = label ?? string.Empty;
            CodeLocation = codeLocation ?? string.Empty;
            CustomPayload = customPayload ?? string.Empty;
        }

        /// <summary>
        /// Determines if this instance equals another instance.
        /// </summary>
        /// <param name="other">The other instance to compare with.</param>
        /// <returns>True if the instances are equal, false otherwise.</returns>
        public bool Equals(CodeCoverageInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            
            return string.Equals(Label, other.Label) &&
                   string.Equals(CodeLocation, other.CodeLocation) &&
                   string.Equals(CustomPayload, other.CustomPayload);
        }

        /// <summary>
        /// Determines if this instance equals another object.
        /// </summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns>True if the object is a CodeCoverageInfo instance and is equal, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            
            return Equals((CodeCoverageInfo)obj);
        }

        /// <summary>
        /// Gets the hash code for this instance.
        /// </summary>
        /// <returns>The hash code value.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Label != null ? Label.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (CodeLocation != null ? CodeLocation.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (CustomPayload != null ? CustomPayload.GetHashCode() : 0);
                return hashCode;
            }
        }
        
        /// <summary>
        /// Gets a string representation of this instance.
        /// </summary>
        /// <returns>A string representation of this instance.</returns>
        public override string ToString()
        {
            return $"Label: '{Label}', Location: '{CodeLocation}', Payload: '{CustomPayload}'";
        }
    }
}
