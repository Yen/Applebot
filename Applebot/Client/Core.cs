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
        private object _pluginLock = new object();

        private List<Task> _platformTasks = new List<Task>();

        public Core()
        {
            ReloadPlugins();
        }

        private void MessageRecievedEventHandler(object sender, Message e)
        {
            IEnumerable<Command> commands = GetCommandsForPlatform(sender as Platform);
            foreach (Command command in commands)
            {
                CalculateLeastDerivedMessageHandle(e.GetType(), sender.GetType(), command.GetType()).Invoke(command, new object[] { e, sender });
            }
        }

        public void StartPlatformTasks()
        {
            if (_platformTasks.Any())
            {
                Logger.Log(Logger.Level.WARNING, "Platform tasks are already running, running them now would be dangerous");
                return;
            }

            foreach (Platform platform in _platforms)
            {
                if (platform.State == PlatformState.Ready)
                    _platformTasks.Add(Task.Run(new Action(platform.Run)));
                else
                    Logger.Log(Logger.Level.WARNING, "Platform \"{0}\" state is not ready, platform will not be started", platform.GetType());
            }
        }

        public void WaitForPlatformTasks()
        {
            while (_platformTasks.Any())
                _platformTasks.First().ContinueWith((Task a) => { _platformTasks.Remove(a); }).Wait();
        }

        private IEnumerable<Command> GetCommandsForPlatform(Platform platform)
        {
            lock (_pluginLock)
            {
                return _commands.Where(x => x.Item2.Contains(platform.GetType()) || x.Item2.Contains(typeof(Platform))).Select(x => x.Item1);
            }
        }

        private MethodInfo CalculateLeastDerivedMessageHandle(Type messageType, Type senderType, Type commandType)
        {
            // Message type takes priority over sender type, this may be changed at a later date

            lock (_pluginLock)
            {
                var methods = commandType.GetMethods().Where(x => x.Name == "HandleMessage").Where(x => x.GetParameters().Length == 2);
                MethodInfo top = typeof(Command).GetMethod("HandleMessage").MakeGenericMethod(new Type[] { messageType, senderType });
                bool hasMessage = false;
                foreach (MethodInfo method in methods)
                {
                    var parameters = method.GetParameters();

                    var a = parameters[1].ParameterType.GetInterfaces();

                    if ((parameters[0].ParameterType == messageType) && (parameters[1].ParameterType == senderType))
                    {
                        return method;
                    }

                    if (!hasMessage && parameters[0].ParameterType == messageType)
                    {
                        if (method.IsGenericMethod)
                        {
                            if (!Enumerable.SequenceEqual(parameters[1].ParameterType.GetInterfaces(), new Type[] { typeof(Platform) }))
                                continue;
                            top = method.MakeGenericMethod(new Type[] { typeof(Platform) });
                        }
                        else
                            top = method;
                        hasMessage = true;
                    }

                    if (!hasMessage && parameters[1].ParameterType == senderType)
                    {
                        if (method.IsGenericMethod)
                        {
                            if (!Enumerable.SequenceEqual(parameters[0].ParameterType.GetInterfaces(), new Type[0]) || !parameters[0].ParameterType.IsSubclassOf(typeof(Message)))
                                continue;
                            top = method.MakeGenericMethod(new Type[] { typeof(Message) });
                        }
                        else
                            top = method;
                    }
                }
                return top;
            }
        }

        public void ReloadPlugins()
        {
            lock (_pluginLock)
            {
                Logger.Log(Logger.Level.APPLICATION, "Reloading plugins");

                _commands.Clear();
                _platforms.Clear();

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

                AssemblyName localAPI = Assembly.GetExecutingAssembly().GetReferencedAssemblies().First(x => x.Name == "ApplebotAPI");

                foreach (Assembly assembly in assemblies)
                {
                    AssemblyName api = assembly.GetReferencedAssemblies().FirstOrDefault(x => x.Name == "ApplebotAPI");
                    if (api == null)
                    {
                        Logger.Log(Logger.Level.WARNING, "Assembly \"{0}\" does not contain a reference to \"{1}\", skipping", assembly.GetName().Name, localAPI);
                        continue;
                    }
                    if (api.Version != localAPI.Version)
                    {
                        Logger.Log(Logger.Level.WARNING, "Assembly \"{0}\" contains a different API version ({1}) to local API version ({2}), please rebuild the plugin to allow for stable use, skipping", assembly.GetName().Name, api.Version, localAPI.Version);
                        continue;
                    }

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
                    if (_commands.Any(x => x.Item1.GetType() == type))
                    {
                        Logger.Log(Logger.Level.ERROR, "A command of type \"{0}\" is already registered, skipping", type);
                        continue;
                    }

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
                        Logger.Log(Logger.Level.WARNING, "Command \"{0}\" has no platform attribute types, the command will never be executed, adding a registrar using the base type \"{1}\" will cause the command to execute on all platforms", command.Name, typeof(Platform));
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
                    if (_platforms.Any(x => x.GetType() == type))
                    {
                        Logger.Log(Logger.Level.ERROR, "A platform of type \"{0}\" is already registered, skipping", type);
                        continue;
                    }

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

                    platform.MessageRecieved += MessageRecievedEventHandler;
                    _platforms.Add(platform);
                    Logger.Log(Logger.Level.APPLICATION, "Platform of type {0} registered", platform.GetType());
                }
            }
        }
    }
}
