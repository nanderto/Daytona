//-----------------------------------------------------------------------
// <copyright file="ZeroMQExtensionMethods.cs" company="The Phantom Coder">
//     Copyright The Phantom Coder. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Daytona
{
    using System.Text;
    using ZeroMQ;

    /// <summary>
    /// Container for extension methods
    /// </summary>
    public static class ZeroMQExtensionMethods
    {
        public static bool Subscribe(this ZmqSocket socket, string input, Encoding encoding)
        {
            if (Encoding.Unicode == encoding)
            {
                var encoder = new UnicodeEncoding();
                socket.Subscribe(encoder.GetBytes(input));
            }

            var defaultencoder = new UnicodeEncoding();
            socket.Subscribe(defaultencoder.GetBytes(input));
            return true;
        }

        ////public static bool Send(this ZmqSocket socket, string message, Encoding encoding, SocketFlags socketFlags)
        ////{
        ////    if (Encoding.Unicode != encoding)
        ////    {
        ////        var encoder = new UTF8Encoding();
        ////        byte[] encoderGetBytes = encoder.GetBytes(message);
        ////        socket.Send(encoderGetBytes, encoderGetBytes.Length, socketFlags);
        ////        return true;
        ////    }

        ////    var defaultencoder = new UnicodeEncoding();
        ////    byte[] defaultencoderGetBytes = defaultencoder.GetBytes(message);
        ////    socket.Send(defaultencoderGetBytes, defaultencoderGetBytes.Length, socketFlags);
        ////    return true;
        ////}
    }
}
