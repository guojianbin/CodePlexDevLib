﻿//-----------------------------------------------------------------------
// <copyright file="DiagnosticsUtilities.cs" company="YuGuan Corporation">
//     Copyright (c) YuGuan Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace DevLib.Diagnostics
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Diagnostics Utilities.
    /// </summary>
    public static class DiagnosticsUtilities
    {
        /// <summary>
        /// WriteLine exception information to console.
        /// </summary>
        /// <param name="exception">Exception instance.</param>
        public static void ConsoleOutputException(Exception exception)
        {
            if (exception != null)
            {
                string message = string.Format(
                    "{0}|{1}|{2}|{3,3}| [{4}] |{5}",
                    DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffUzzz", CultureInfo.InvariantCulture),
                    "EXCP",
                    Environment.UserName,
                    Thread.CurrentThread.ManagedThreadId,
                    exception.ToString(),
                    GetStackFrameInfo(1));

                Debug.WriteLine(message);
                Console.WriteLine(message);
            }
        }

        /// <summary>
        /// Builds a readable representation of the method in which the frame is executing.
        /// </summary>
        /// <param name="stackFrame">Instance of <see cref="T:System.Diagnostics.StackFrame" />, which represents a function call on the call stack for the current thread.</param>
        /// <returns>A readable representation of the method in which the frame is executing.</returns>
        public static string GetStackFrameMethodInfo(StackFrame stackFrame)
        {
            if (stackFrame == null)
            {
                return string.Empty;
            }

            MethodBase methodBase = stackFrame.GetMethod();

            if (methodBase != null)
            {
                StringBuilder stringBuilder = new StringBuilder(255);

                Type declaringType = methodBase.DeclaringType;

                if (declaringType != null)
                {
                    stringBuilder.Append(declaringType.FullName.Replace('+', '.'));
                    stringBuilder.Append(".");
                }

                stringBuilder.Append(methodBase.Name);

                if (methodBase is MethodInfo && ((MethodInfo)methodBase).IsGenericMethod)
                {
                    Type[] genericArguments = ((MethodInfo)methodBase).GetGenericArguments();

                    stringBuilder.Append("[");

                    int i = 0;

                    bool flag = true;

                    while (i < genericArguments.Length)
                    {
                        if (!flag)
                        {
                            stringBuilder.Append(",");
                        }
                        else
                        {
                            flag = false;
                        }

                        stringBuilder.Append(genericArguments[i].Name);

                        i++;
                    }

                    stringBuilder.Append("]");
                }

                stringBuilder.Append("(");

                ParameterInfo[] parameters = methodBase.GetParameters();

                bool flag2 = true;

                for (int j = 0; j < parameters.Length; j++)
                {
                    if (!flag2)
                    {
                        stringBuilder.Append(", ");
                    }
                    else
                    {
                        flag2 = false;
                    }

                    string parameterTypeName = "<UnknownType>";

                    if (parameters[j].ParameterType != null)
                    {
                        parameterTypeName = parameters[j].ParameterType.Name;
                    }

                    stringBuilder.Append(parameterTypeName + " " + parameters[j].Name);
                }

                stringBuilder.Append(")");

                return stringBuilder.ToString();
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds a readable representation of the stack trace.
        /// </summary>
        /// <param name="skipFrames">The number of frames up the stack to skip.</param>
        /// <returns>A readable representation of the stack trace.</returns>
        public static string GetStackFrameInfo(int skipFrames)
        {
            StackFrame stackFrame = new StackFrame(skipFrames < 1 ? 1 : skipFrames + 1, true);

            MethodBase method = stackFrame.GetMethod();

            if (method != null)
            {
                StringBuilder stringBuilder = new StringBuilder();

                stringBuilder.Append(method.Name);

                if (method is MethodInfo && ((MethodInfo)method).IsGenericMethod)
                {
                    Type[] genericArguments = ((MethodInfo)method).GetGenericArguments();

                    stringBuilder.Append("<");

                    int i = 0;

                    bool flag = true;

                    while (i < genericArguments.Length)
                    {
                        if (!flag)
                        {
                            stringBuilder.Append(",");
                        }
                        else
                        {
                            flag = false;
                        }

                        stringBuilder.Append(genericArguments[i].Name);

                        i++;
                    }

                    stringBuilder.Append(">");
                }

                stringBuilder.Append(" in ");

                stringBuilder.Append(Path.GetFileName(stackFrame.GetFileName()) ?? "<unknown>");

                stringBuilder.Append(":");

                stringBuilder.Append(stackFrame.GetFileLineNumber());

                return stringBuilder.ToString();
            }
            else
            {
                return "<null>";
            }
        }
    }
}
