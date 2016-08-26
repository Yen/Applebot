using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApplebotEx
{
    public enum LoggerColor
    {
        Default,
        Black,
        DarkBlue,
        DarkGreen,
        DarkCyan,
        DarkRed,
        DarkMagenta,
        DarkYellow,
        Gray,
        DarkGray,
        Blue,
        Green,
        Cyan,
        Red,
        Magenta,
        Yellow,
        White
    }

    public interface ILogger
    {
        object Lock { get; }

        void Log(string value, LoggerColor color = LoggerColor.Default);
    }
}
