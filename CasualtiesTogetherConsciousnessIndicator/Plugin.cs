using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace CasualtiesTogetherConsciousnessIndicator;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("CasualtiesMP", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
    public const string ModGuid = MyPluginInfo.PLUGIN_GUID;
    public const string ModName = MyPluginInfo.PLUGIN_NAME;
    public const string ModVersion = MyPluginInfo.PLUGIN_VERSION;

    internal new static ManualLogSource Logger;

    private readonly Harmony _harmony = new(ModGuid);

    public static Plugin Instance { get; private set; } = null!;

    public static bool MpModLoaded = false;
    public static Type MpModNetBody;
    public static Type MpModScavPlayer;
    public static Type MpModColor24;
    public static MethodInfo MpModNetworkIsRunningGetter;
    public static MethodInfo MpModIsPlayerGetter;
    public static MethodInfo MpModPlayerGetter;
    public static FieldInfo MpModPlayerColorField;
    public static MethodInfo MpModToColorWithAlpha;

    internal static IndicatorSettings Unconscious;
    internal static IndicatorSettings Sleeping;

    public static string TextureDir;

    private float _nextCheckTime = 0f;

    public void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        EnsureManagerObjectHidden();

        foreach (var assembly in AccessTools.AllAssemblies())
        {
            if (!assembly.GetName().Name.Equals("CasualtiesMP"))
                continue;

            if (TryLoadingMpModFields(assembly))
                MpModLoaded = true;
            else
                PrintError("Failed to load Casualties: Together integration!");

            break;
        }

        TextureDir = Path.Combine(Paths.PluginPath, $"{ModName}");
        Directory.CreateDirectory(TextureDir);

        Unconscious = new IndicatorSettings(Config, "General", "!!.png");
        Sleeping = new IndicatorSettings(Config, "Sleeping", "zzz.png");

        _harmony.PatchAll();

        Logger.LogInfo($"Plugin {ModName} is loaded!");
    }

    public void Update()
    {
        if (Unconscious.Enabled.Value)
            Unconscious.Icon.ReloadIfNeeded(Unconscious.IconFile.Value);
        if (Sleeping.Enabled.Value)
            Sleeping.Icon.ReloadIfNeeded(Sleeping.IconFile.Value);
    }

    public void LateUpdate()
    {
        if (!Unconscious.Enabled.Value && !Sleeping.Enabled.Value)
            return;

        if (Time.realtimeSinceStartup < _nextCheckTime)
            return;
        _nextCheckTime = Time.realtimeSinceStartup + 3f;

        foreach (var body in FindObjectsByType<Body>(FindObjectsSortMode.None))
        {
            var character = body.transform.parent.gameObject;
            if (character.TryGetComponent<PlayerConsciousnessIndicator>(out _))
                continue;

            object netPlayer = null;
            if (MpModLoaded && (bool)MpModNetworkIsRunningGetter.Invoke(null, null))
            {
                var netBody = body.GetComponent(MpModNetBody);
                if (netBody == null)
                    continue;
                var isPlayer = (bool)MpModIsPlayerGetter.Invoke(netBody, null);
                if (!isPlayer)
                    continue;
                netPlayer = MpModPlayerGetter.Invoke(netBody, null);
            }

            var indicator = character.AddComponent<PlayerConsciousnessIndicator>();
            indicator.body = body;
            indicator.netPlayer = netPlayer;
        }
    }

    internal static void PrintWarning(string message)
    {
        Logger.LogWarning(message);
        ConsoleScript.instance?.LogToConsole($"<color=yellow>[{Plugin.ModName}] {message}</color>");
    }

    internal static void PrintError(string message)
    {
        Logger.LogError(message);
        ConsoleScript.instance?.LogToConsole($"<color=red>[{Plugin.ModName}] {message}</color>");
    }

    private static bool TryLoadingMpModFields(Assembly assembly)
    {
        try
        {
            MpModNetBody = GetType(assembly, $"{nameof(Together)}.{nameof(Together.NetBody)}");
            MpModScavPlayer = GetType(assembly, $"{nameof(Together)}.{nameof(Together.ScavPlayer)}");
            MpModColor24 = GetType(assembly, $"{nameof(Together)}.{nameof(Together.Color24)}");
            var mpModNet = GetType(assembly, $"{nameof(Together)}.{nameof(Together.Net)}");
            MpModNetworkIsRunningGetter = GetGetter(mpModNet, nameof(Together.Net.IsRunning));
            MpModIsPlayerGetter = GetGetter(MpModNetBody, nameof(Together.NetBody.IsPlayer));
            MpModPlayerGetter = GetGetter(MpModNetBody, nameof(Together.NetBody.plr));
            MpModPlayerColorField = GetField(MpModScavPlayer, nameof(Together.ScavPlayer.playerColor));
            MpModToColorWithAlpha = GetMethod(MpModColor24, nameof(Together.Color24.ToColorWithAlpha), [typeof(float)]);
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Exception thrown while obtaining MP mod references: " + ex.Message);
            return false;
        }
        return true;
    }

    private static Type GetType(Assembly assembly, string name)
    {
        var type = assembly.GetType(name, throwOnError: false);
        return type ?? throw new NullReferenceException($"Couldn't find type \"{name}\"!");
    }

    private static MethodInfo GetGetter(Type type, string name)
    {
        var getter = AccessTools.PropertyGetter(type, name);
        return getter ?? throw new NullReferenceException($"Couldn't find getter \"{name}\"!");
    }

    private static FieldInfo GetField(Type type, string name)
    {
        var field = AccessTools.Field(type, name);
        return field ?? throw new NullReferenceException($"Couldn't find field \"{name}\"!");
    }

    private static MethodInfo GetMethod(Type type, string name, Type[] parameters)
    {
        var method = AccessTools.Method(type, name, parameters);
        return method ?? throw new NullReferenceException($"Couldn't find method \"{name}\"!");
    }

    private static void EnsureManagerObjectHidden()
    {
        var config = new ConfigFile(Paths.BepInExConfigPath, false)
        {
            SaveOnConfigSet = false
        };
        var entry = config.Bind("Chainloader", "HideManagerGameObject", false,
            "If enabled, hides BepInEx Manager GameObject from Unity.");
        if (entry.Value)
            return;
        Instance.gameObject.hideFlags = HideFlags.HideAndDontSave;
        entry.Value = true;
        config.Save();
    }
}
