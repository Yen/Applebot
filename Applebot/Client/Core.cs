using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApplebotAPI;
using System.IO;
using System.Reflection;

namespace ClientNew
{
    class Core
    {
        private List<Tuple<Command, IEnumerable<Type>>> _commands = new List<Tuple<Command, IEnumerable<Type>>>();
        private List<Platform> _platforms = new List<Platform>();

        public Core()
        {
            if (!Directory.Exists("Plugins"))
            {
                Logger.Log(Logger.Level.WARNING, "Could not fild plugins folder, no plugins will be loaded");
                return;
            }

            Logger.Log(Logger.Level.APPLICATION, "Searching plugins folder for assemblies...");

            string[] assemblyPaths = Directory.GetFiles("Plugins", "*.dll");

            if (assemblyPaths.Length == 0)
            {
                Logger.Log(Logger.Level.APPLICATION, "No possible assemblies were found in plugins folder");
                return;
            }

            Logger.Log(Logger.Level.APPLICATION, "A total of {0} possible {1} was found in plugins folder", assemblyPaths.Length, (assemblyPaths.Length == 1) ? "assembly" : "assemblies");

            List<Assembly> assemblies = new List<Assembly>();
            foreach (string path in assemblyPaths)
            {
                try
                {
                    AssemblyName name = AssemblyName.GetAssemblyName(path);
                    Assembly assembly = Assembly.Load(name);
                    assemblies.Add(assembly);
                }
                catch (BadImageFormatException e)
                {
                    Logger.Log(Logger.Level.WARNING, "File \"{0}\" in plugins folder is not a valid assembly, skipping", e.FileName);
                    continue;
                }
            }

            Logger.Log(Logger.Level.APPLICATION, "A total of {0} valid {1} loaded", assemblies.Count, (assemblies.Count == 1) ? "assembly was" : "assemblies were");

            List<Type> types = new List<Type>();

            foreach (Assembly assembly in assemblies)
            {
                var t = assembly.GetTypes();
                if (t.Length == 0)
                {
                    Logger.Log(Logger.Level.WARNING, "No types were found in assembly \"{0}\", plugin is empty", assembly.GetName().Name);
                    continue;
                }
                types.AddRange(assembly.GetTypes());
            }

            if (types.Count == 0)
            {
                Logger.Log(Logger.Level.WARNING, "No types were found in any loaded assembly, no plugins will be loaded");
                return;
            }
            Logger.Log(Logger.Level.APPLICATION, "A total of {0} valid {1} found in assemblies", types.Count, (types.Count == 1) ? "type was" : "types were");

            List<Type> commandTypes = new List<Type>();
           
            foreach (Type type in types)
            {
                if (type.IsSubclassOf(typeof(Command)))
                {
                    commandTypes.Add(type);
                }
            }

            Logger.Log(Logger.Level.APPLICATION, "Of {0} total {1}, {2} {3} found to be a subclass of {4}", types.Count, (types.Count == 1) ? "type" : "types", commandTypes.Count, (commandTypes.Count == 1) ? "was" : "were", typeof(Command));

            foreach (Type type in commandTypes)
            {
                ConstructorInfo constructor = type.GetConstructor(new Type[0]);
                if (constructor == null || !constructor.IsPublic)
                {
                    Logger.Log(Logger.Level.ERROR, "Command type \"{0}\" does not have a valid public constructor, skipping", type);
                    continue;
                }

                Command command = null;
                try
                {
                    command = Activator.CreateInstance(type) as Command;
                }
                catch
                {
                    Logger.Log(Logger.Level.ERROR, "There was an error creating Command from type \"{0}\", type was skipped");
                    continue;
                }

                IEnumerable<PlatformRegistrar> platformAttributes = type.GetCustomAttributes<PlatformRegistrar>(true).Distinct();

                if (platformAttributes.Count() == 0)
                {
                    Logger.Log(Logger.Level.WARNING, "Command \"{0}\" has no platform attribute types, the command will never be executed", command.Name);
                }

                IEnumerable<Type> platformList = platformAttributes.Select(x => x.PlatformType);

                _commands.Add(Tuple.Create(command, platformList));
                Logger.Log(Logger.Level.APPLICATION, "Command \"{0}\" of type {1} registered", command.Name, command.GetType());
            }

            List<Type> platformTypes = new List<Type>();

            foreach (Type type in types)
            {
                if (type.IsSubclassOf(typeof(Platform)))
                {
                    platformTypes.Add(type);
                }
            }

            Logger.Log(Logger.Level.APPLICATION, "Of {0} total {1}, {2} {3} found to be a subclass of {4}", types.Count, (types.Count == 1) ? "type" : "types", platformTypes.Count, (platformTypes.Count == 1) ? "was" : "were", typeof(Platform));

            foreach (Type type in platformTypes)
            {
                ConstructorInfo constructor = type.GetConstructor(new Type[0]);
                if (constructor == null || !constructor.IsPublic)
                {
                    Logger.Log(Logger.Level.ERROR, "Platform type \"{0}\" does not have a valid public constructor, skipping", type);
                    continue;
                }

                Platform platform = null;
                try
                {
                    platform = Activator.CreateInstance(type) as Platform;
                }
                catch
                {
                    Logger.Log(Logger.Level.ERROR, "There was an error creating Platform from type \"{0}\", type was skipped");
                    continue;
                }

                _platforms.Add(platform);
                Logger.Log(Logger.Level.APPLICATION, "Platform of type {0} registered", platform.GetType());
            }
        }
    }
}
