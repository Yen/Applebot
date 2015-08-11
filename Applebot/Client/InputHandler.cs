using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Client
{
    class InputHandler
    {

        private BotCore _core;
        private BotSettings _settings;
        private List<Function> _functions = new List<Function>();

        public InputHandler(BotCore core, BotSettings settings)
        {
            _core = core;
            _settings = settings;

            //TODO: load functions
            Type[] defaults = typeof(DefaultFunctions).GetNestedTypes();
            foreach (Type type in defaults)
            {
                Type[] interfaces = type.GetInterfaces();
                if (interfaces.Contains(typeof(Function)))
                {
                    Function function = (Function)Activator.CreateInstance(type);
                    Logger.Log(Logger.Level.LOG, "Loaded function \"{0}\" from default functions list", function.Name);
                    _functions.Add(function);
                    continue;
                }
                Logger.Log(Logger.Level.WARNING, "Type \"{0}\" in default functions does not implement Function, remove it", type.Name);
            }
        }

        private Function Check(string expression)
        {
            foreach (Function function in _functions)
            {
                if (expression.Equals(function.Expression))
                    return function;
            }
            return null;
        }

        public void Run()
        {
            while (true)
            {
                string line;
                while ((line = Console.ReadLine()) != null)
                {
                    string[] parts = line.Split(' ');

                    Function function = Check(parts[0]);
                    if (function != null)
                    {
                        Logger.Log(Logger.Level.INPUT, "Function \"{0}\" ({1}) triggered", function.Name, function.Expression);
                        function.Execute(_core, _settings, parts.Skip(1).ToArray());
                    }
                    else
                    {
                        Logger.Log(Logger.Level.INPUT, "Unknown function \"{0}\"", parts[0]);
                    }
                }
            }
        }

    }
}
