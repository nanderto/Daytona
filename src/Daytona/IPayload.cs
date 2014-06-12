//-----------------------------------------------------------------------
// <copyright file="IPayload.cs" company="The Phantom Coder">
//     Copyright The Phantom Coder. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Daytona
{
    using System;

    /// <summary>
    /// IPayload interface provides access to treat data handled by system as a payload
    /// </summary>
    public interface IPayload
    {
    }

    /// <summary>
    /// IPayload interface that allows the data to be treated generically 
    /// </summary>
    /// <typeparam name="T">The type of data that is the payload</typeparam>
    public interface IPayload<T>
    {
        T Payload { get; set; }
    }

    [Serializable]
    public class MessagePayload<T> : IPayload, IPayload<T>
    {
        private object[] args;
        public MessagePayload(object[] args)
        {
            this.args = args;
        }

        public int Id { get; set; }

        public T Payload { get; set; }
    }
}