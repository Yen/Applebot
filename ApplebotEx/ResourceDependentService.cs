using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ApplebotEx
{
    public abstract class ResourceDependentService<T> : Service
    {
        private string _ResourcePath;
        protected object ResourceLock = new object();
        protected T Resource;

        protected ResourceDependentService(string resourcePath)
        {
            _ResourcePath = resourcePath;
        }

        public sealed override bool Initialize()
        {
            // load resource
            if (!ReloadResource())
                return false;

            Logger.Log($"Loaded resource file -> {_ResourcePath}");

            // if resource loading succeeds, bootstrap the service
            return Bootstrap();
        }

        protected bool ReloadResource()
        {
            try
            {
                if (!File.Exists(_ResourcePath))
                {
                    Logger.Log($"Resource file does not exist -> {_ResourcePath}");
                    return false;
                }

                var res = JsonConvert.DeserializeObject<T>(File.ReadAllText(_ResourcePath));

                lock (ResourceLock)
                    Resource = res;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error loading resource -> {ex.Message}");
                return false;
            }

            return true;
        }

        protected bool SaveResource()
        {
            lock (ResourceLock)
                try
                {
                    var source = JsonConvert.SerializeObject(Resource, Formatting.Indented);
                    File.WriteAllText(_ResourcePath, source);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to save resource -> {ex.Message}");
                    return false;
                }
            return true;
        }

        protected virtual bool Bootstrap() => true;
    }
}
