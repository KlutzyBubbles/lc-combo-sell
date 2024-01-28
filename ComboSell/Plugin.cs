using BepInEx;
using HarmonyLib;
using BepInEx.Logging;
using BepInEx.Configuration;
using System.IO;
using System;
using Newtonsoft.Json;

namespace ComboSell
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource StaticLogger;

        public static bool debug = false;

        private Harmony _harmony;

        private readonly string configPath = Path.Combine(Paths.ConfigPath, "combos.json");

        private void Awake()
        {
            StaticLogger = Logger;
            StaticLogger.LogInfo("ComboSell loading...");
            ConfigFile();
            _harmony = new Harmony("ComboSell");
            if (debug)
            {
                _harmony.PatchAll(typeof(DebugPatch));
            }
            _harmony.PatchAll(typeof(CompanyCounterPatch));
            StaticLogger.LogInfo("ComboSell loaded");
        }

        private void ConfigFile()
        {
            ConfigEntry<bool> configDebug = Config.Bind("Dev", "Debug", false, "Whether or not to enable debug logging and debug helpers");
            debug = configDebug.Value;

            ConfigEntry<bool> configRemoveUnknown = Config.Bind("Settings", "Remove unknown items", false, "Whether or not to remove unknown item names when loading the config, this doesn't write to the json");

            if (!File.Exists(configPath))
            {
                CompanyCounterPatch.settings = new ComboSettings(configRemoveUnknown.Value);
                File.WriteAllText(configPath, JsonConvert.SerializeObject(CompanyCounterPatch.settings));
            }
            else
            {
                try
                {
                    CompanyCounterPatch.settings = ComboSettings.FromJson(File.ReadAllText(configPath), configRemoveUnknown.Value);
                }
                catch (Exception e)
                {
                    StaticLogger.LogError(e);
                    CompanyCounterPatch.settings = new ComboSettings(configRemoveUnknown.Value);
                }
            }
        }

        public static void Debug(string message)
        {
            if (debug) StaticLogger.LogDebug(message);
        }
    }
}