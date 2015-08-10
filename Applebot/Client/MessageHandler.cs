using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    class MessageHandler
    {
        private BotSettings _settings;
        private BotCore _sender;

        private List<Command> _commands = new List<Command>();

        public MessageHandler(BotSettings settings, BotCore sender)
        {
            _settings = settings;
            _sender = sender;

            LoadCommands();
        }

        public void Execute(string user, string message)
        {
            if ((bool)_settings["loggingMessages"])
                Logger.Log(Logger.Level.MESSAGE, "{0}: {1}", user, message);

            Command command = Check(message);
            if (command != null)
            {
                Logger.Log(Logger.Level.COMMAND, "User \"{0}\" triggered command ({1})", user, command.Name);
                new Thread(() => { command.Execute(user, message, _sender, _settings); }).Start();
            }
        }

        private Command Check(string message)
        {
            foreach (Command command in _commands)
            {
                foreach (Regex regex in command.Expressions)
                {
                    if (regex.Match(message).Success)
                    {
                        return command;
                    }
                }
            }
            return null;
        }

        private void LoadCommands()
        {
            Logger.Log(Logger.Level.LOG, "Loading command dlls");

            if (!Directory.Exists("Commands"))
            {
                Logger.Log(Logger.Level.WARNING, "Unable to locate commands folder, no commands will be loaded");
                return;
            }

            string[] files = Directory.GetFiles("Commands", "*.dll");

            Logger.Log(Logger.Level.LOG, "Located ({0}) possible command dll", files.Length);

            List<Assembly> assemblies = new List<Assembly>();
            foreach (string file in files)
            {
                AssemblyName name = AssemblyName.GetAssemblyName(file);
                Assembly assembly = Assembly.Load(name);
                assemblies.Add(assembly);
            }

            foreach (Assembly assembly in assemblies)
            {
                Type[] types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    Type[] interfaces = type.GetInterfaces();
                    if (interfaces.Contains(typeof(Command)))
                    {
                        Command command = (Command)Activator.CreateInstance(type);
                        if (_commands.Any(i => i.Name == command.Name))
                        {
                            Logger.Log(Logger.Level.ERROR, "Command named \"{0}\" was already loaded, not loading new command", command.Name);
                            break;
                        }
                        Logger.Log(Logger.Level.LOG, "Loaded command \"{0}\" from dll \"{1}\"", command.Name, assembly.GetName().Name);
                        _commands.Add(command);
                    }
                }
            }
        }

    }
}
