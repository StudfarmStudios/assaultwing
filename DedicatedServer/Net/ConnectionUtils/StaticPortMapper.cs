/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using NATUPNPLib;

namespace AW2.Net.ConnectionUtils
{
    public static class StaticPortMapper
    {
        public static bool IsSupported { get { return Mappings != null; } }

        private static string MappingDescription { get { return "Assault Wing game server"; } }

        private static IStaticPortMappingCollection Mappings
        {
            get
            {
                var upnpNat = new UPnPNATClass();
                return upnpNat.StaticPortMappingCollection;
            }
        }

        public static void EnsurePortMapped(int port, string protocol)
        {
            if (!IsSupported) throw new InvalidOperationException("Static port mapping is not supported by the network device");
            var mapping = GetMappingOrNull(port, protocol);
            if (mapping == null)
                AddMapping(port, protocol);
            else
                CheckMapping(port, mapping);
        }

        public static void RemovePortMapping(int port, string protocol)
        {
            if (!IsSupported) throw new InvalidOperationException("Static port mapping is not supported by the network device");
            var mapping = GetMappingOrNull(port, protocol);
            if (mapping == null) return;
            Mappings.Remove(port, protocol);
        }

        private static IStaticPortMapping GetMappingOrNull(int port, string protocol)
        {
            return Mappings
                .Cast<IStaticPortMapping>()
                .FirstOrDefault(m => m.ExternalPort == port && m.Protocol == protocol);
        }

        private static void AddMapping(int port, string protocol)
        {
            Mappings.Add(port, protocol, port, GetLocalIPAddress(), true, MappingDescription);
        }

        private static void CheckMapping(int port, IStaticPortMapping mapping)
        {
            var localIP = GetLocalIPAddress();
            if (mapping.Description != MappingDescription) mapping.EditDescription(MappingDescription);
            if (mapping.InternalPort != port) mapping.EditInternalPort(port);
            if (mapping.InternalClient != localIP) mapping.EditInternalClient(localIP);
            if (!mapping.Enabled) mapping.Enable(true);
        }

        private static string GetLocalIPAddress()
        {
            var localHostName = Dns.GetHostName();
            var localHostEntry = Dns.GetHostEntry(localHostName);
            var localIPAddress = localHostEntry.AddressList.First(
                address => address.AddressFamily == AddressFamily.InterNetwork);
            return localIPAddress.ToString();
        }
    }
}
*/