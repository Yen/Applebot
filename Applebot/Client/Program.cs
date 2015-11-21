//#define DEV //Development

using System;
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

#if DEV
            WebPanel panel = new WebPanel();
            panel.Run();
#else
            Core core = new Core();

            core.StartPlatformTasks();
            core.WaitForPlatformTasks();
#endif

            Console.WriteLine("Program ended");
            Console.ReadKey();
        }
    }
}
