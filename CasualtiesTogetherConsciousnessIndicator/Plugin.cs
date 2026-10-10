using System;
using System.IO;
using System.Reflection;
using BepInEx;
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
    public static Type MpModCon;
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

    private float _t = 0f;

    public void Awake()
    {
        Logger = base.Logger;
        Instance = this;

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

    public void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        Unconscious?.Icon.Dispose();
        Sleeping?.Icon.Dispose();
        Instance = null;
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

        _t += Time.unscaledDeltaTime;
        if (_t < 3f)
            return;
        _t = 0f;

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
        if (!TryGetType(assembly, $"{nameof(Together)}.{nameof(Together.Con)}", out MpModCon))
            return false;
        if (!TryGetType(assembly, $"{nameof(Together)}.{nameof(Together.NetBody)}", out MpModNetBody))
            return false;
        if (!TryGetType(assembly, $"{nameof(Together)}.{nameof(Together.ScavPlayer)}", out MpModScavPlayer))
            return false;
        if (!TryGetType(assembly, $"{nameof(Together)}.{nameof(Together.Color24)}", out MpModColor24))
            return false;
        if (!TryGetType(assembly, $"{nameof(Together)}.{nameof(Together.Net)}", out Type mpModNet))
            return false;
        if (!TryGetGetter(mpModNet, nameof(Together.Net.IsRunning), out MpModNetworkIsRunningGetter))
            return false;
        if (!TryGetGetter(MpModNetBody, nameof(Together.NetBody.IsPlayer), out MpModIsPlayerGetter))
            return false;
        if (!TryGetGetter(MpModNetBody, nameof(Together.NetBody.plr), out MpModPlayerGetter))
            return false;
        if (!TryGetField(MpModScavPlayer, nameof(Together.ScavPlayer.playerColor), out MpModPlayerColorField))
            return false;
        if (!TryGetMethod(MpModColor24, nameof(Together.Color24.ToColorWithAlpha), [typeof(float)], out MpModToColorWithAlpha))
            return false;
        return true;
    }

    public static bool TryGetType(Assembly assembly, string name, out Type type)
    {
        type = assembly.GetType(name, throwOnError: false);
        return type != null;
    }

    public static bool TryGetGetter(Type type, string name, out MethodInfo getter)
    {
        getter = AccessTools.PropertyGetter(type, name);
        return getter != null;
    }

    public static bool TryGetField(Type type, string name, out FieldInfo field)
    {
        field = AccessTools.Field(type, name);
        return field != null;
    }

    public static bool TryGetMethod(Type type, string name, Type[] parameters, out MethodInfo method)
    {
        method = AccessTools.Method(type, name, parameters);
        return method != null;
    }
}
