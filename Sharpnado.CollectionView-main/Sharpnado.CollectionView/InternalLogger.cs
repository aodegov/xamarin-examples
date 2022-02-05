﻿using System;
using System.Threading;

namespace Sharpnado.CollectionView
{
    public static class InternalLogger
    {
        public static bool EnableLogging { get; private set; }

        public static bool EnableDebug { get; private set; }

        public static void EnableLogger(bool enableGlobalLogging, bool enableDebugLevel)
        {
            EnableLogging = enableGlobalLogging;
            EnableDebug = enableDebugLevel;
        }

        public static void Debug(string tag, Func<string> message)
        {
            if (!EnableDebug)
            {
                return;
            }

            DiagnosticLog(tag + " | DBUG | " + message());
        }

        public static void Debug(string tag, string format, params object[] parameters)
        {
            if (!EnableDebug)
            {
                return;
            }

            DiagnosticLog(tag + " | DBUG | " + format, parameters);
        }

        public static void Debug(string format, params object[] parameters)
        {
            if (!EnableDebug)
            {
                return;
            }

            DiagnosticLog("DBUG | " + format, parameters);
        }

        public static void Info(string tag, string format, params object[] parameters)
        {
            DiagnosticLog(tag + " | INFO | " + format, parameters);
        }

        public static void Info(string format, params object[] parameters)
        {
            DiagnosticLog("INFO | " + format, parameters);
        }

        public static void Warn(string format, params object[] parameters)
        {
            DiagnosticLog("WARN | " + format, parameters);
        }

        public static void Error(string format, params object[] parameters)
        {
            DiagnosticLog("ERRO | " + format, parameters);
        }

        public static void Error(Exception exception)
        {
            Error($"{exception.Message}{Environment.NewLine}{exception}");
        }

        private static void DiagnosticLog(string format, params object[] parameters)
        {
            if (!EnableLogging)
            {
                return;
            }

#if DEBUG
            System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString("MM-dd H:mm:ss.fff") + " | SharpnadoInternals | " + $"{Thread.CurrentThread.ManagedThreadId:000} | " + format, parameters);
#else
            Console.WriteLine(DateTime.Now.ToString("MM-dd H:mm:ss.fff") + " | SharpnadoInternals | " + format, parameters);
#endif
        }
    }
}