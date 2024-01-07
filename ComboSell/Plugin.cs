using BepInEx;
using HarmonyLib;
using BepInEx.Logging;
using BepInEx.Configuration;

namespace ComboSell
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource StaticLogger;

        public static bool debug = false;

        private Harmony _harmony;

        private void Awake()
        {
            StaticLogger = Logger;
            StaticLogger.LogInfo("ComboSell loading...");
            ConfigFile();
            CompanyCounterPatch.settings = new ComboSettings();
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
            /*
            EmotePatch.enabledList = new bool[EmoteDefs.getEmoteCount() + 1];
            EmotePatch.defaultKeyList = new string[EmoteDefs.getEmoteCount() + 1];
            EmotePatch.defaultControllerList = new string[EmoteDefs.getEmoteCount() + 1];
            foreach (string name in Enum.GetNames(typeof(Emote)))
            {
                if (EmoteDefs.getEmoteNumber(name) > 2)
                {
                    ConfigEntry<string> keyConfig = Config.Bind("Emote Keys", $"{name} Key", $"<Keyboard>/{EmoteDefs.getEmoteNumber(name)}", $"Default keybind for {name} emote");
                    EmotePatch.defaultKeyList[EmoteDefs.getEmoteNumber(name)] = keyConfig.Value.Equals("") ? "" : (keyConfig.Value.ToLower().StartsWith("<keyboard>") ? keyConfig.Value : $"<Keyboard>/{keyConfig.Value}");
                    ConfigEntry<string> controllerConfig = Config.Bind("Emote Controller Bindings", $"{name} Button", "", $"Default controller binding for {name} emote");
                    EmotePatch.defaultControllerList[EmoteDefs.getEmoteNumber(name)] = controllerConfig.Value.Equals("") ? "" : (controllerConfig.Value.ToLower().StartsWith("<gamepad>") ? controllerConfig.Value : $"<Gamepad>/{controllerConfig.Value}");
                }
                ConfigEntry<bool> enabledConfig = Config.Bind("Enabled Emotes", $"Enable {name}", true, $"Toggle {name} emote key");
                EmotePatch.enabledList[EmoteDefs.getEmoteNumber(name)] = enabledConfig.Value;
            }
            ConfigEntry<string> configEmoteKey = Config.Bind("Emote Keys", "Emote Wheel Key", "<Keyboard>/v", "Default keybind for the emote wheel");
            EmotePatch.emoteWheelKey = configEmoteKey.Value.Equals("") ? "" : (configEmoteKey.Value.ToLower().StartsWith("<keyboard>") ? configEmoteKey.Value : $"<Keyboard>/{configEmoteKey.Value}");
            ConfigEntry<string> configEmoteController = Config.Bind("Emote Controller Bindings", "Emote Wheel Button", "<Gamepad>/leftShoulder", "Default controller binding for the emote wheel");
            EmotePatch.emoteWheelController = configEmoteController.Value.Equals("") ? "" : (configEmoteController.Value.ToLower().StartsWith("<gamepad>") ? configEmoteController.Value : $"<Gamepad>/{configEmoteController.Value}");
            ConfigEntry<string> configEmoteControllerMove = Config.Bind("Emote Controller Bindings", "Emote Wheel Move", "<Gamepad>/rightStick", "Default controller binding for the emote wheel movement");
            EmotePatch.emoteWheelControllerMove = configEmoteControllerMove.Value.Equals("") ? "" : (configEmoteControllerMove.Value.ToLower().StartsWith("<gamepad>") ? configEmoteControllerMove.Value : $"<Gamepad>/{configEmoteControllerMove.Value}");
            ConfigEntry<float> configEmoteControllerDeadzone = Config.Bind("Emote Controller Bindings", "Emote Wheel Deadzone", 0.25f, "Default controller deadzone for emote selection");
            SelectionWheel.controllerDeadzone = configEmoteControllerDeadzone.Value < 0 ? 0 : configEmoteControllerDeadzone.Value;

            ConfigEntry<float> configGriddySpeed = Config.Bind("Emote Settings", "Griddy Speed", 0.5f, "Speed of griddy relative to regular speed");
            EmotePatch.griddySpeed = configGriddySpeed.Value < 0 ? 0 : configGriddySpeed.Value;

            ConfigEntry<float> configEmoteCooldown = Config.Bind("Emote Settings", "Cooldown", 0.5f, "Time (in seconds) to wait before being able to switch emotes");
            EmotePatch.emoteCooldown = configEmoteCooldown.Value < 0 ? 0 : configEmoteCooldown.Value;

            ConfigEntry<bool> configEmoteStop = Config.Bind("Emote Settings", "Stop on outer", false, "Whether or not to stop emoting when mousing to outside the emote wheel");
            EmotePatch.stopOnOuter = configEmoteStop.Value;
            */
        }

        public static void Debug(string message)
        {
            if (debug) StaticLogger.LogDebug(message);
        }
    }
}