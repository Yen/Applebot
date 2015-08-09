using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    static class Logger
    {

        private static readonly object _lock = new object();
        private static int _count;

        public struct Level
        {
            public string output;
            public ConsoleColor color;
        }

        public static class GenericLevels
        {
            public static readonly Level LOG = new Level { output = "Log", color = ConsoleColor.Gray };
            public static readonly Level ERROR = new Level { output = "Error", color = ConsoleColor.Red };
            public static readonly Level WARNING = new Level { output = "Warning", color = ConsoleColor.Yellow };
        }

        public static void Log(Level level, string format, params object[] keys)
        {
            string buffer = string.Format(format, keys);
            lock (_lock)
            {
                Console.Write("[{0}][{1}]<", _count, DateTime.Now.ToString());

                Console.ForegroundColor = level.color;
                Console.Write("{0}", level.output);
                Console.ResetColor();

                Console.WriteLine("> : {0}", buffer);
                _count++;
            }
        }

    }
}
