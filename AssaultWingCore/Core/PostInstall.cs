using System;
using System.Deployment.Application;
using System.IO;
using System.Xml;
using AW2.Helpers;

namespace AW2.Core
{
    /// <summary>
    /// Performs post-install actions.
    /// </summary>
    public static class PostInstall
    {
        public static void CreateDedicatedServerShortcut()
        {
            if (!ApplicationDeployment.IsNetworkDeployed || !ApplicationDeployment.CurrentDeployment.IsFirstRun) return;
            Log.Write("Creating a shortcut for dedicated server in Start Menu");
            var programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            var publisherAndProduct = GetPublisherAndProduct();
            if (programs == null) return;
            var folderPath = Path.Combine(programs, publisherAndProduct.Item1);
            var launcherPath = Path.Combine(folderPath, "Assault Wing Dedicated Server.cmd");
            var productPath = Path.Combine(folderPath, publisherAndProduct.Item2 + ".appref-ms");
            using (var writer = File.CreateText(launcherPath))
            {
                writer.WriteLine("@echo off");
                writer.WriteLine("echo dedicated_server > \"{0}\"", AssaultWingCore.ArgumentPath);
                writer.WriteLine("\"{0}\"", productPath);
            }
        }

        private static Tuple<string, string> GetPublisherAndProduct()
        {
            var deploymentManifestFilename = ApplicationDeployment.CurrentDeployment.UpdateLocation.AbsoluteUri;
            using (var reader = new XmlTextReader(deploymentManifestFilename))
            {
                reader.MoveToContent();
                reader.ReadToDescendant("description");
                var publisher = reader.GetAttribute("asmv2:publisher");
                var product = reader.GetAttribute("asmv2:product");
                return Tuple.Create(publisher, product);
            }
        }
    }
}
