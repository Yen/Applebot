using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplebotAPI
{

    /// <summary>
    /// An attribute used for selecting what platforms a command should activate on
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class PlatformRegistrar : Attribute
    {
        public Type PlatformType { get; private set; }

        /// <param name="platformType">The type of the platform that the command should activate on</param>
        public PlatformRegistrar(Type platformType)
        {
            PlatformType = platformType;
            if (!PlatformType.IsSubclassOf(typeof(Platform)))
            {
                throw new ArgumentException(string.Format("Argument must be a subclass of {0}", typeof(Platform)), "platformType");
            }
        }
    }

    public enum PlatformState
    {
        Ready,
        Unready
    }

    /// <summary>
    /// Base class for all platfrom type plugins
    /// </summary>
    public abstract class Platform
    {
        /// <summary>
        /// An event that is called when the platform recieves a message that should be passed onto the command plugins
        /// </summary>
        public event EventHandler<Message> MessageRecieved;

        /// <summary>
        /// The state of the platform and it's connections
        /// </summary>
        public PlatformState State { get; protected set; } = PlatformState.Ready;

        /// <summary>
        /// A method that handles incoming requests to send data out to the platforms connections
        /// </summary>
        /// <typeparam name="T1">A object of the class hierarchy SendData</typeparam>
        /// <param name="data">The data that was requested to be sent</param>
        public abstract void Send<T1>(T1 data)
            where T1 : SendData;

        /// <summary>
        /// The main loop of the platform
        /// </summary>
        public abstract void Run();

        /// <summary>
        /// Hands a message off to the client to send to the required command plugins
        /// </summary>
        /// <param name="platform">Should almost always be set to "this", represents the platform object that
        /// recieved the message and the one that will be asked to send responses</param>
        /// <param name="message">The formatted message that will be given to commands</param>
        protected void ProcessMessage(Platform platform, Message message)
        {
            MessageRecieved(platform, message);
        }

        /// <summary>
        /// Checks whether a sender has elevated status to this backend
        /// </summary>
        /// <param name="sender">The sender that is being queried, this can be null if the command has
        /// no specific sender entity</param>
        /// <returns>This method returns false by default and should return false if there is no
        /// specific elevated users on this backend</returns>
        public virtual bool CheckElevatedStatus(string sender)
        {
            return false;
        }
    }

    /// <summary>
    /// The base class for all outgoing data to be sent by a platform
    /// </summary>
    public class SendData
    {
        /// <summary>
        /// A representation of what should be sent
        /// </summary>
        public string Content { get; private set; }

        /// <summary>
        /// If elevated the command should get a priority when the message is being sent
        /// </summary>
        public bool Elevated { get; private set; }

        public Message Origin { get; private set; }

        /// <param name="content">A string that represets the data to be sent</param>
        /// <param name="elevated">If the data should have elevated status when being sent</param>
        public SendData(string content, bool elevated, Message origin)
        {
            Content = content;
            Elevated = elevated;
            Origin = origin;
        }
    }
}
