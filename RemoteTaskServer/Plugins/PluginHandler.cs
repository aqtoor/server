﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using MiscUtil.IO;
using UlteriusPluginBase;
using UlteriusServer.Forms.Utilities;

namespace UlteriusServer.Plugins
{
    public class PluginHandler
    {
        public static Dictionary<string, PluginBase> Plugins;
        public static Dictionary<string, List<string>> PluginPermissions;
        public static Dictionary<string, string> ApprovedPlugins;
        public static Dictionary<string, string> PendingPlugins;
        private static readonly List<string> BadPlugins = new List<string>();


        public static object StartPlugin(string guid, List<object> args = null)
        {
            try
            {
                var plugin = Plugins[guid];
                return args != null ? plugin.Start(args) : plugin.Start();
            }
            catch (SecurityException e)
            {
                var input = e.Message;
                var start = input.IndexOf("'", StringComparison.Ordinal) + 1;
                var end = input.IndexOf(",", start, StringComparison.Ordinal);
                var result = input.Substring(start, end - start);
                var message = $"Plugin attempted to invoke code that it lacks permissions for: {result}";
                var pluginResponse = new
                {
                    permissionError = true,
                    message
                };
                return pluginResponse;
            }
            catch (Exception ex)
            {
                var pluginResponse = new
                {
                    pluginError = true,
                    message = ex.Message
                };
                return pluginResponse;
            }
        }

        public static void SetupPlugin(string guid)
        {
            try
            {
                var plugin = Plugins[guid];
                if (plugin.RequiresSetup)
                {
                    plugin.Setup();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static int GetTotalPlugins()
        {
            return Plugins.Count;
        }

        public static List<string> GetBadPluginsList()
        {
            return BadPlugins;
        }


        public static void LoadPlugins()
        {
            if (!File.Exists(UlteriusServer.Plugins.PluginPermissions.TrustFile))
            {
                File.Create(UlteriusServer.Plugins.PluginPermissions.TrustFile).Close();
            }
            Plugins = new Dictionary<string, PluginBase>();
            PluginPermissions = new Dictionary<string, List<string>>();
            ApprovedPlugins = new Dictionary<string, string>();
            PendingPlugins = new Dictionary<string, string>();
            foreach (var line in new LineReader(() => new StringReader(UlteriusServer.Plugins.PluginPermissions.GetApprovedGuids())))
            {
                var pluginData = line.Split('|');
                var name = pluginData[0];
                var guid = pluginData[1];
                ApprovedPlugins.Add(name, guid);
            }
            var plugins = PluginLoader.LoadPlugins();
            if (plugins != null)
            {
                BadPlugins.AddRange(PluginLoader.BrokenPlugins);
                foreach (var plugin in plugins)
                {
                    var pluginApproved = true;
                    if (!ApprovedPlugins.ContainsValue(plugin.GUID.ToString()))
                    {
                        pluginApproved = false;
                        try
                        {
                            Console.WriteLine(plugin.GUID.ToString());
                            PendingPlugins.Add(plugin.Name, plugin.GUID.ToString());
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                    //probably a better way to expose objects
                    plugin.NotificationIcon = UlteriusTray.NotificationIcon;
                    Plugins.Add(plugin.GUID.ToString(), plugin);
                    if (pluginApproved)
                    {
                        SetupPlugin(plugin.GUID.ToString());
                    }
                }
            }
        }
    }
}