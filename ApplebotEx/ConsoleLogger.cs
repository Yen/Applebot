using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApplebotEx
{
    public class ConsoleLogger: ILogger
    {
        private string _Display;

        public object Lock { get { return Console.Out; } }

        public ConsoleLogger(string display = null)
        {
            _Display = display;
        }

        public void Log(string value, LoggerColor color)
        {
            lock (Console.Out)
            {
                Console.Write($"[{DateTime.Now.ToString("HH:mm:ss")}]");
                if (_Display != null)
                    Console.Write($"[{_Display}]");
                Console.Write(" ");
                if (color != LoggerColor.Default)
                {
                    switch (color)
                    {
                        case LoggerColor.Black:
                            Console.ForegroundColor = ConsoleColor.Black;
                            break;
                        case LoggerColor.DarkBlue:
                            Console.ForegroundColor = ConsoleColor.DarkBlue;
                            break;
                        case LoggerColor.DarkGreen:
                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            break;
                        case LoggerColor.DarkCyan:
                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                            break;
                        case LoggerColor.DarkRed:
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            break;
                        case LoggerColor.DarkMagenta:
                            Console.ForegroundColor = ConsoleColor.DarkMagenta;
                            break;
                        case LoggerColor.DarkYellow:
                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            break;
                        case LoggerColor.Gray:
                            Console.ForegroundColor = ConsoleColor.Gray;
                            break;
                        case LoggerColor.DarkGray:
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            break;
                        case LoggerColor.Blue:
                            Console.ForegroundColor = ConsoleColor.Blue;
                            break;
                        case LoggerColor.Green:
                            Console.ForegroundColor = ConsoleColor.Green;
                            break;
                        case LoggerColor.Cyan:
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            break;
                        case LoggerColor.Red:
                            Console.ForegroundColor = ConsoleColor.Red;
                            break;
                        case LoggerColor.Magenta:
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            break;
                        case LoggerColor.Yellow:
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            break;
                        case LoggerColor.White:
                            Console.ForegroundColor = ConsoleColor.White;
                            break;
                    }
                    Console.WriteLine(value);
                    Console.ResetColor();
                }
                else
                    Console.WriteLine(value);
            }
        }
    }
}
