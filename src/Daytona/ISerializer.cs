//-----------------------------------------------------------------------
// <copyright file="ISerializer.cs" company="The Phantom Coder">
//     Copyright The Phantom Coder. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Daytona
{
    using System;
    using System.Text;
    
    public interface ISerializer
    {
        Encoding Encoding { get; }

        T Deserializer<T>(byte[] input);

        T Deserializer<T>(string input);

        object Deserializer(string input, Type type);

        object Deserializer(byte[] input, Type type);

        byte[] GetBuffer<T>(T message);

        string GetString(byte[] buffer);

        string GetString<T>(T message);
    }
}