﻿public static class Log
{
    enum LogType { Verb, Stream, Info, Warn, Error }
    private static readonly object logLockObj = new();

    public static void Stream(string text, bool newLine = true)
    {
        lock (logLockObj)
        {
            FormatColorWrite(text, ConsoleColor.Green, newLine);
        }
    }

    public static void Info(string text, bool newLine = true)
    {
        lock (logLockObj)
        {
            FormatColorWrite(text, ConsoleColor.DarkYellow, newLine);
        }
    }

    public static void Warn(string text, bool newLine = true)
    {
        lock (logLockObj)
        {
            FormatColorWrite(text, ConsoleColor.DarkMagenta, newLine);
        }
    }

    public static void Error(string text, bool newLine = true)
    {
        lock (logLockObj)
        {
            FormatColorWrite(text, ConsoleColor.DarkRed, newLine);
        }
    }

    public static void Error(Exception ex, string text, bool newLine = true)
    {
        lock (logLockObj)
        {
            FormatColorWrite(text, ConsoleColor.DarkRed, newLine);
            FormatColorWrite(ex.ToString(), ConsoleColor.DarkRed, newLine);
        }
    }

    public static void FormatColorWrite(string text, ConsoleColor consoleColor = ConsoleColor.Gray, bool newLine = true)
    {
        text = $"[{DateTime.Now:yyyy/MM/dd HH:mm:ss}] {text}";
        Console.ForegroundColor = consoleColor;
        if (newLine) Console.WriteLine(text);
        else Console.Write(text);
        Console.ForegroundColor = ConsoleColor.Gray;
    }

    public static Task LogMsg(LogMessage message)
    {
        ConsoleColor consoleColor = ConsoleColor.DarkCyan;

        switch (message.Severity)
        {
            case LogSeverity.Error:
                consoleColor = ConsoleColor.DarkRed;
                break;
            case LogSeverity.Warning:
                consoleColor = ConsoleColor.DarkMagenta;
                break;
            case LogSeverity.Debug:
                consoleColor = ConsoleColor.Green;
                break;
        }

#if DEBUG
        if (!string.IsNullOrEmpty(message.Message)) FormatColorWrite(message.Message, consoleColor);
#endif

        if (message.Exception != null && message.Exception is not GatewayReconnectException)
        {
            consoleColor = ConsoleColor.DarkRed;
            FormatColorWrite(message.Exception.GetType().FullName, consoleColor);
            FormatColorWrite(message.Exception.Message, consoleColor);
            FormatColorWrite(message.Exception.StackTrace, consoleColor);
        }

        return Task.CompletedTask;
    }
}