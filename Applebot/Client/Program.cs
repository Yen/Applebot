﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Applebot");

            Core core = new Core();

            core.StartPlatformTasks();
            core.WaitForPlatformTasks();

            Console.WriteLine("Program ended");
            Console.ReadKey();
        }
    }
}
