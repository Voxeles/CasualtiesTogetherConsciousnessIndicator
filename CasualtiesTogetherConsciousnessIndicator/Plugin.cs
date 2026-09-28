using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace CasualtiesTogetherConsciousnessIndicator;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("KrokoshaCasualtiesMP", BepInDependency.DependencyFlags.SoftDependency)]
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
    public static Type MpModNetPlayer;
    public static Type MpModColor24;
    public static MethodInfo MpModNetworkIsRunningGetter;
    public static MethodInfo MpModIsPlayerGetter;
    public static MethodInfo MpModNetPlayerGetter;
    public static FieldInfo MpModNetPlayerColorField;
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
            if (!assembly.GetName().Name.Equals("KrokoshaCasualtiesMP"))
                continue;
            MpModLoaded = true;
            var mpModScavMultiplayer = assembly.GetType($"{nameof(KrokoshaCasualtiesMP)}.{nameof(KrokoshaCasualtiesMP.KrokoshaScavMultiplayer)}", true);
            MpModCon = assembly.GetType($"{nameof(KrokoshaCasualtiesMP)}.{nameof(KrokoshaCasualtiesMP.Con)}", true);
            MpModNetBody = assembly.GetType($"{nameof(KrokoshaCasualtiesMP)}.{nameof(KrokoshaCasualtiesMP.NetBody)}", true);
            MpModNetPlayer = assembly.GetType($"{nameof(KrokoshaCasualtiesMP)}.{nameof(KrokoshaCasualtiesMP.NetPlayer)}", true);
            MpModColor24 = assembly.GetType($"{nameof(KrokoshaCasualtiesMP)}.{nameof(KrokoshaCasualtiesMP.Color24)}", true);
            MpModNetworkIsRunningGetter = AccessTools.PropertyGetter(mpModScavMultiplayer, nameof(KrokoshaCasualtiesMP.KrokoshaScavMultiplayer.network_system_is_running));
            MpModIsPlayerGetter = AccessTools.PropertyGetter(MpModNetBody, nameof(KrokoshaCasualtiesMP.NetBody.is_player));
            MpModNetPlayerGetter = AccessTools.PropertyGetter(MpModNetBody, nameof(KrokoshaCasualtiesMP.NetBody.player));
            MpModNetPlayerColorField = MpModNetPlayer.GetField("playerColor", AccessTools.all) ?? MpModNetPlayer.GetField("plrcolor", AccessTools.all);
            MpModToColorWithAlpha = AccessTools.Method(MpModColor24, nameof(KrokoshaCasualtiesMP.Color24.ToColorWithAlpha), [typeof(float)]);
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
                netPlayer = MpModNetPlayerGetter.Invoke(netBody, null);
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
}
