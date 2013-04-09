//-----------------------------------------------------------------------
// <copyright file="Utils.cs" company="The Phantom Coder">
//     Copyright The Phantom Coder. All rights reserved.
// </copyright>
//-----------------------------------------------------------------
namespace Phantom.PubSub
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    /// <summary>
    /// OPtions to determin if store will support transactions or not
    /// </summary>
    public enum StoreTransactionOption
    {
        /// <summary>
        /// The support transactions
        /// </summary>
        SupportTransactions,

        /// <summary>
        /// Does not support transactions
        /// </summary>
        NoTransactions
    }

    /// <summary>
    /// Utils utility class for Debugging / tracing
    /// </summary>
    public static class Utils
    {
        public static readonly TraceSwitch GeneralSwitch = new TraceSwitch("General", "Entire Application");
        public static readonly TraceSwitch EsentSwitch = new TraceSwitch("Esent", "Esent Store");
        public static readonly TraceSwitch MsmqSwitch = new TraceSwitch("Msmq", "Msmq Store");

        public static string OutputStack()
        {
            StackTrace stackTrace = new StackTrace();
            StackFrame stackFrame;
            MethodBase stackFrameMethod;
            string typeName = string.Empty;
            string newTypeName;
            StringBuilder sb = null;
            sb = new StringBuilder();

            int position = 0;

            for (int x = position; x < stackTrace.FrameCount; x++)
            {
                stackFrame = stackTrace.GetFrame(x);

                stackFrameMethod = stackFrame.GetMethod();

                newTypeName = stackFrameMethod.ReflectedType.FullName;
                if (newTypeName != typeName)
                {
                    sb.AppendLine();
                    sb.AppendLine(newTypeName);
                    typeName = newTypeName;
                }

                sb.Append(stackFrameMethod);
                sb.Append(" ");
            }

            return sb.ToString();
        }
    }
}
