using System;
using Microsoft.SPOT;

namespace NetduinoCore
{
    public enum LogLevel : int
    {
        Info = 1,
        Error = 2,
        Warning = 3,
        Verbose = 4,
    }

    class NDLogger
    {
        private static int LOG_LEVEL = 1;
        private static NDLogger[] loggers;

        public static void Log(string text, LogLevel level = LogLevel.Info)
        {
            if (LOG_LEVEL > 0 && (int)level <= LOG_LEVEL)
            {
                foreach (NDLogger logger in loggers)
                {
                    logger.handler(text);
                }
            }
        }

        public static void SetLogLevel(LogLevel logLevel)
        {
            LOG_LEVEL = (int)logLevel;
        }

        public static void RemoveLoggers()
        {
            loggers = null;
        }

        public static void AddLogger(NDLogger logger)
        {
            if (loggers == null)
            {
                loggers = new NDLogger[] { logger };
            }
            else
            {
                NDLogger[] newLoggers = new NDLogger[loggers.Length + 1];
                Array.Copy(loggers, newLoggers, loggers.Length);
                newLoggers[loggers.Length] = logger;

                loggers = newLoggers;
            }
        }

        public static void EnableLogging()
        {
            LOG_LEVEL = 1;
        }

        public static void DisableLogging()
        {
            LOG_LEVEL = 0;
        }

        public delegate void LogMessageHandler(string message);
        protected LogMessageHandler handler;

        protected NDLogger()
        {
        }

        public NDLogger(LogMessageHandler handler)
        {
            this.handler = handler;
        }
    }

    class NDTTYLogger : NDLogger
    {
        public NDTTYLogger()
        {
            this.handler = logToConsole;
        }

        private void logToConsole(string message)
        {
            Debug.Print(message);
        }
    }
}
