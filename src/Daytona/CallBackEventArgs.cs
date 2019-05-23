//-----------------------------------------------------------------------
// <copyright file="CallBackEventArgs.cs" company="The Phantom Coder">
//     Copyright The Phantom Coder. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Daytona
{
    using System;
    using System.Collections.Generic;

    public class CallBackEventArgs : EventArgs
    {
        public Exception Error { get; set; }
        
        public bool Cancelled { get; set; }
        
        public bool Result { get; set; }
        
        public List<IPayload> Payload { get; set; }
    }
}
