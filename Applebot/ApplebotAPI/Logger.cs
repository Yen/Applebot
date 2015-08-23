using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplebotAPI
{
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static int _count = 0;

        private static bool _color = true;

        public sealed class Level
        {
            public string Output { get; private set; }
            public ConsoleColor Color { get; private set; }

            private Level(string output, ConsoleColor color)
            {
                Output = output;
                Color = color;
            }

            public static readonly Level ERROR = new Level("Error", ConsoleColor.Red);
            public static readonly Level WARNING = new Level("Warning", ConsoleColor.Yellow);
            public static readonly Level APPLICATION = new Level("App", ConsoleColor.Magenta);
        }

        public static void Log(Level level, string format, params object[] args)
        {
            lock (_lock)
            {
                Console.Write("[{0}][{1}]", _count++, DateTime.Now);

                if (level != null)
                {
                    if (!_color)
                        Console.Write("<{0}>", level.Output);
                    else
                    {
                        Console.Write("<");
                        Console.ForegroundColor = level.Color;
                        Console.Write("{0}", level.Output);
                        Console.ResetColor();
                        Console.Write(">");
                    }
                }

                Console.WriteLine(": {0}", string.Format(format, args));
            }
        }

        public static void Log(string format, params object[] args)
        {
            Log(null, format, args);
        }
    }
}
