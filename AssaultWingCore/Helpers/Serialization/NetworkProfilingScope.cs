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
            if (obj is Gob)
            {
                ProfilingNetworkBinaryWriter.Push("Gob: " + ((Gob)obj).TypeName);
            }
            else
            {
                ProfilingNetworkBinaryWriter.Push(obj.GetType().Name);
            }
        }

        public void Dispose()
        {
            ProfilingNetworkBinaryWriter.Pop();
        }
    }
}
