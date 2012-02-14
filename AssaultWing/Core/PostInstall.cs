using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Win32;
using AW2.Helpers;

namespace AW2.Core
{
    /// <summary>
    /// Performs post-install actions.
    /// </summary>
    public static class PostInstall
    {
        private const string WEB_LAUNCHER_DLL_NAME = "npAssaultWingLauncher.dll";
        private static string DedicatedServerScriptPath { get { return Path.Combine(ProductStartFolder, ProductWithFlavour + " Dedicated Server.cmd"); } }
        private static string UninstallScriptPath { get { return Path.Combine(ProductStartFolder, "Uninstall " + ProductWithFlavour + ".cmd"); } }
        private static string WebLauncherDllPath { get { return Path.Combine(MiscHelper.DataDirectory, WEB_LAUNCHER_DLL_NAME); } }
        private static string Publisher { get { return GetManifestDescriptionElement("asmv2:publisher"); } }
        private static string Product { get { return GetManifestDescriptionElement("asmv2:product"); } }
        private static string ProductWithFlavour { get { return "Assault Wing" + ProductFlavourSuffix; } }
        private static string ProductFlavourSuffix
        {
            get
            {
                var match = Regex.Matches(Product, @"\(.*\)").OfType<Match>().FirstOrDefault();
                return match == null ? "" : " " + match.Groups[0].Value;
            }
        }
        private static string ProductLauncher { get { return Path.Combine(ProductStartFolder, Product + ".appref-ms"); } }
        private static string ProductStartFolder { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), Publisher); } }
        private static string UninstallString { get { return GetMatchingUninstallStrings().SingleOrDefault() ?? ""; } }

        public static void EnsureDone()
        {
            if (!MiscHelper.IsNetworkDeployed || !ApplicationDeployment.CurrentDeployment.IsFirstRun) return;
            Log.Write("Creating shortcuts under Start Menu");
            CreateScript(DedicatedServerScriptPath, WriteDedicatedServerShortcut);
            CreateScript(UninstallScriptPath, WriteUninstallerShortcut);
            Log.Write("Registering web launcher DLL");
            RegisterWebLauncherDll();
        }

        private static void CreateScript(string path, Action<StreamWriter> write)
        {
            using (var writer = File.CreateText(path)) write(writer);
        }

        private static void WriteDedicatedServerShortcut(StreamWriter writer)
        {
            writer.WriteLine("@echo off");
            writer.WriteLine("echo dedicated_server > \"{0}\"", AssaultWingCore.ArgumentPath);
            writer.WriteLine("\"{0}\"", ProductLauncher);
        }

        private static void WriteUninstallerShortcut(StreamWriter writer)
        {
            writer.WriteLine("@echo off");
            writer.WriteLine("regsvr32.exe /s /u \"{0}\"", WebLauncherDllPath);
            writer.WriteLine(UninstallString);
            // At this point, the DLL is officially uninstalled.
            // However, There may still be traces of the DLL in Windows registry.
            // They can be cleaned away with CCleaner of similar software.
            writer.WriteLine("del \"{0}\"", DedicatedServerScriptPath);
            writer.WriteLine("del \"{0}\"", UninstallScriptPath); // Self-deletion must be the last line in the uninstaller.
        }

        private static void RegisterWebLauncherDll()
        {
            // Copy the DLL to the data directory and register it there. Install directory will change
            // at every upgrade but the data directory stays.
            File.Copy(WEB_LAUNCHER_DLL_NAME, WebLauncherDllPath, true);
            Process.Start("regsvr32.exe", string.Format("/s /i \"{0}\"", WebLauncherDllPath));
        }

        private static IEnumerable<string> GetMatchingUninstallStrings()
        {
            // Adapted from Jim Harte’s Blog (http://www.jamesharte.com/blog/?p=11) on 2012-02-12.
            using (var uninstallKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall"))
                foreach (var appKeyName in uninstallKey.GetSubKeyNames())
                    using (var appKey = uninstallKey.OpenSubKey(appKeyName))
                        if (appKey.GetString("DisplayName") == Product && appKey.GetString("Publisher") == Publisher)
                            yield return appKey.GetString("UninstallString");
        }

        private static string GetManifestDescriptionElement(string childName)
        {
            var deploymentManifestFilename = ApplicationDeployment.CurrentDeployment.UpdateLocation.AbsoluteUri;
            using (var reader = new XmlTextReader(deploymentManifestFilename))
            {
                reader.MoveToContent();
                reader.ReadToDescendant("description");
                return reader.GetAttribute(childName);
            }
        }
    }
}
