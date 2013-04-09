//-----------------------------------------------------------------------
// <copyright file="EsentDatabaseException.cs" company="The Phantom Coder">
//     Copyright The Phantom Coder. All rights reserved.
// </copyright>
//----------------------------------------------------------------------
namespace Daytona.Store
{ 
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// EsentDatabaseException rasied from interactions with ESENT
    /// </summary>
    [Serializable]
    public class EsentDatabaseException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EsentDatabaseException" /> class.
        /// </summary>
        public EsentDatabaseException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EsentDatabaseException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public EsentDatabaseException(string message)
            : base(message) 
        { 
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EsentDatabaseException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="ex">The ex.</param>
        public EsentDatabaseException(string message, Exception ex)
            : base(message, ex) 
        { 
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EsentDatabaseException" /> class.
        /// </summary>
        /// <param name="serializationInfo">The serialization info.</param>
        /// <param name="streamingContext">The streaming context.</param>
        protected EsentDatabaseException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext) 
        { 
        }
    }
}
