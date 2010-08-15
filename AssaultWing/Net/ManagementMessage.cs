using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AW2.Net
{
    /// <summary>
    /// A network message between a management server and a game instance (client or server).
    /// </summary>
    public abstract class ManagementMessage : Message
    {
        private static Dictionary<string, Type> g_subclasses;

        protected virtual string[] Parameters { get { return new string[0]; } }

        private string OperationText
        {
            get
            {
                var attribute = (ManagementMessageAttribute)GetType().GetCustomAttributes(typeof(ManagementMessageAttribute), false).First();
                return "operation=" + attribute.Operation;
            }
        }

        private string ParameterText
        {
            get
            {
                var textBuilder = new StringBuilder();
                foreach (var param in Parameters)
                {
                    textBuilder.Append(";");
                    textBuilder.Append(param);
                }
                return textBuilder.ToString();
            }
        }

        private string Text { get { return OperationText + ParameterText; } }

        static ManagementMessage()
        {
            var subclassData =
                from type in Assembly.GetExecutingAssembly().GetTypes()
                where typeof(ManagementMessage).IsAssignableFrom(type) && !type.IsAbstract
                let attribute = (ManagementMessageAttribute)type.GetCustomAttributes(typeof(ManagementMessageAttribute), false).First()
                select new { attribute.Operation, type };
            g_subclasses = subclassData.ToDictionary(pair => pair.Operation, pair => pair.type);
        }

        protected ManagementMessage()
        {
        }

        protected static List<Dictionary<string, string>> Tokenize(string message)
        {
            return message.Split('\n')
                .Select(line => line.Split(';')
                    .Select(token => token.Split('='))
                    .Where(tokenSplit => tokenSplit.Length >= 2)
                    .ToDictionary(parts => parts[0], parts => parts[1]))
                .ToList();
        }

        public new static ManagementMessage Deserialize(byte[] data, int byteCount)
        {
            string text = Encoding.ASCII.GetString(data, 0, byteCount);
            var tokens = Tokenize(text);
            var operation = tokens[0]["operation"];
            var subclass = GetSubclass(operation);
            var message = (ManagementMessage)Activator.CreateInstance(subclass);
            message.Deserialize(tokens);
            return message;
        }

        private static Type GetSubclass(string operation)
        {
            return g_subclasses[operation];
        }

        public override byte[] Serialize()
        {
            return Encoding.ASCII.GetBytes(Text);
        }

        protected abstract void Deserialize(List<Dictionary<string, string>> tokenizedLines);

        protected override void Serialize(NetworkBinaryWriter writer)
        {
            throw new NotImplementedException();
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            throw new NotImplementedException();
        }
    }
}
