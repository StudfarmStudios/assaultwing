using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AW2.Game;

namespace AW2.Helpers.Serialization
{
    public class NetworkProfilingScope : IDisposable
    {
        public NetworkProfilingScope(string name)
        {
            ProfilingNetworkBinaryWriter.Push(name);
        }

        public NetworkProfilingScope(object obj)
        {
            if (obj == null) throw new ArgumentNullException();
            ProfilingNetworkBinaryWriter.Push(
                obj is string ? (string)obj :
                obj is Gob ? "Gob: " + ((Gob)obj).TypeName :
                obj.GetType().Name);
        }

        public void Dispose()
        {
            ProfilingNetworkBinaryWriter.Pop();
        }
    }
}
