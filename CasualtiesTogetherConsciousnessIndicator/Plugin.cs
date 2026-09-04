using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

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

	public static ConfigEntry<bool> ConfigEnabled;
	public static ConfigEntry<string> ConfigIconFile;
	public static ConfigEntry<bool> ConfigDoRotate;
	public static ConfigEntry<float> ConfigScale;
	public static ConfigEntry<bool> ConfigDoTint;

	public static string TextureDir;
	public static byte[] FallbackImage;
	public static Texture2D FallbackTexture;

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
			MpModNetPlayerColorField = AccessTools.Field(MpModNetPlayer, nameof(KrokoshaCasualtiesMP.NetPlayer.plrcolor));
			MpModToColorWithAlpha = AccessTools.Method(MpModColor24, nameof(KrokoshaCasualtiesMP.Color24.ToColorWithAlpha), [typeof(float)]);
			break;
		}

		TextureDir = Path.Combine(Paths.PluginPath, $"{ModName}");
		Directory.CreateDirectory(TextureDir);

		ConfigEnabled = Config.Bind(
			"General",
			"Enabled",
			true,
			"Set to true to enable the consciousness indicator");
		ConfigIconFile = Config.Bind(
			"General",
			"IconFile",
			"zzz.png",
			"Which file within BepInEx/plugins/ConsciousnessIndicator to use as the icon");
		ConfigDoRotate = Config.Bind(
			"General",
			"DoRotate",
			false,
			"Set to true to create three rotating icons around an unconscious player");
		ConfigScale = Config.Bind(
			"General",
			"Scale",
			6f,
			"The scale of the icons");
		ConfigDoTint = Config.Bind(
			"General",
			"DoTint",
			true,
			"Set to true to tint the icons with the player's color");

		LoadFallbackTexture();

		_harmony.PatchAll();

		Logger.LogInfo($"Plugin {ModName} is loaded!");
	}

	public void OnDestroy()
	{
		_harmony?.UnpatchSelf();
		Instance = null;
	}

	public void LateUpdate()
	{
		if (!ConfigEnabled.Value)
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

	private static void LoadFallbackTexture()
	{
		const string assetName = "CasualtiesTogetherConsciousnessIndicator.assets.fallback.png";
		try
		{
			var assembly = Assembly.GetExecutingAssembly();
			using (Stream manifestResourceStream = assembly.GetManifestResourceStream(assetName))
			{
				if (manifestResourceStream == null)
					throw new Exception("manifestResourceStream is null");

				var assetBytes = new byte[manifestResourceStream.Length];
				var read = manifestResourceStream.Read(assetBytes, 0, assetBytes.Length);
				if (read != assetBytes.Length)
					throw new Exception("read fewer bytes than expected");

				FallbackImage = assetBytes;
			}

			FallbackTexture = new Texture2D(2, 2);
			FallbackTexture.LoadImage(FallbackImage);
			FallbackTexture.filterMode = FilterMode.Point;
		}
		catch (Exception ex)
		{
			FallbackImage = Texture2D.whiteTexture.EncodeToPNG();
			FallbackTexture = Texture2D.whiteTexture;
			Logger.LogError($"Failed to load asset {assetName}: " + ex);
		}
	}
}

internal class PlayerConsciousnessIndicator : MonoBehaviour
{
	public Body body;
	public object netPlayer;
	private GameObject _icon1;
	private GameObject _icon2;
	private GameObject _icon3;
	private Vector2 _pos;
	private float _t;
	private bool _myDoRotate;
	private float _myScale;
	private Color _myColor;

	private static Texture2D _sIconTexture;
	private static GameObject _sIconPrefab;
	private static float _sLastCheckTime = 0f;
	private static DateTime _sLastWriteTime = DateTime.MinValue;
	private static List<PlayerConsciousnessIndicator> _sInstances = [];

	private static readonly Vector3 Icon1Dir = Quaternion.Euler(0f, 0f, -15f) * Vector2.right * 1.5f;
	private static readonly Vector3 Icon1Axis = Quaternion.Euler(0f, 0f, 90f) * Icon1Dir;
	private static readonly Vector3 Icon2Dir = Quaternion.Euler(0f, 0f, -20f) * Vector2.right * 1.5f;
	private static readonly Vector3 Icon2Axis = Quaternion.Euler(0f, 0f, 90f) * Icon2Dir;
	private static readonly Vector3 Icon3Dir = Quaternion.Euler(0f, 0f, -10f) * Vector2.right * 1.5f;
	private static readonly Vector3 Icon3Axis = Quaternion.Euler(0f, 0f, 90f) * Icon3Dir;

	private Color GetPlayerColor()
	{
		if (netPlayer == null)
			return Color.white;

		var color24 = Plugin.MpModNetPlayerColorField.GetValue(netPlayer);
		return (Color)Plugin.MpModToColorWithAlpha.Invoke(color24, [1.0f]);
	}

	private void Start()
	{
		try
		{
			_t = Random.value;
			_myColor = Plugin.ConfigDoTint.Value ? GetPlayerColor() : Color.white;
			_myScale = Plugin.ConfigScale.Value;
			_myDoRotate = Plugin.ConfigDoRotate.Value;
			_pos = (Vector2)body.limbs[0].transform.position + Vector2.up * 10f;
			EnsureIconPrefab(true);
			InitIcons();
			_sInstances.Add(this);
		}
		catch (Exception ex)
		{
			Plugin.Logger.LogWarning("PlayerConsciousnessIcon couldn't Start(): " + ex);
			Destroy(this);
		}
	}

	private void LateUpdate()
	{
		if (!body || !Plugin.ConfigEnabled.Value)
		{
			Destroy(this);
			return;
		}

		EnsureIconPrefab();

		if (body.conscious && !_icon1.activeSelf)
			return;

		if (!body.alive)
		{
			if (!_icon1.activeSelf)
				return;
			_icon1.SetActive(false);
			_icon2.SetActive(false);
			_icon3.SetActive(false);
			return;
		}

		UpdatePrefs();

		_t += Time.deltaTime;
		if (_t > 1)
			_t = 0;

		var headPos = (Vector2)body.limbs[0].transform.position;

		if (body.conscious)
		{
			var leavePos = headPos + Vector2.up * 4f;
			_pos = Vector2.Lerp(_pos, leavePos, Time.deltaTime * 10f);

			if (Mathf.Abs(_pos.y - leavePos.y) <= 1f)
			{
				_icon1.SetActive(false);
				_icon2.SetActive(false);
				_icon3.SetActive(false);
				return;
			}
		}
		else
		{
			if (!_icon1.activeSelf)
			{
				_pos = headPos + Vector2.up * 4f;
				_icon1.SetActive(true);
				_icon2.SetActive(_myDoRotate);
				_icon3.SetActive(_myDoRotate);
			}
			var desired = headPos + Vector2.up * 1.5f;
			_pos = Vector2.Lerp(_pos, desired, Time.deltaTime * 5f);
		}

		UpdateIcons();
	}

	private void UpdateIcons()
	{
		var pos = _pos;

		if (!Plugin.ConfigDoRotate.Value)
		{
			_icon1.transform.position = pos;
		}
		else
		{
			var angle = _t * 360f;
			_icon1.transform.position = (Vector3)pos + Quaternion.AngleAxis(angle, Icon1Axis) * Icon1Dir;
			_icon2.transform.position = (Vector3)pos + Quaternion.AngleAxis(angle + 120f, Icon2Axis) * Icon2Dir;
			_icon3.transform.position = (Vector3)pos + Quaternion.AngleAxis(angle + 240f, Icon3Axis) * Icon3Dir;
		}
	}

	private void UpdatePrefs()
	{
		var doRotate = Plugin.ConfigDoRotate.Value;
		if (_myDoRotate != doRotate)
		{
			_myDoRotate = doRotate;
			if (_icon1.activeSelf)
			{
				_icon2.SetActive(doRotate);
				_icon3.SetActive(doRotate);
			}
		}

		var scale = Plugin.ConfigScale.Value;
		if (!Mathf.Approximately(_myScale, scale))
		{
			_myScale = scale;
			_icon1.transform.localScale = new Vector3(scale, scale, 0);
			_icon2.transform.localScale = new Vector3(scale + 0.5f, scale + 0.5f, 0);
			_icon3.transform.localScale = new Vector3(scale - 0.5f, scale - 0.5f, 0);
		}

		var color = Plugin.ConfigDoTint.Value ? GetPlayerColor() : Color.white;
		if (_myColor != color)
		{
			_myColor = color;
			_icon1.GetComponent<SpriteRenderer>().color = color;
			_icon2.GetComponent<SpriteRenderer>().color = color;
			_icon3.GetComponent<SpriteRenderer>().color = color;
		}
	}

	private void OnDestroy()
	{
		_sInstances.Remove(this);
		Destroy(_icon1);
		Destroy(_icon2);
		Destroy(_icon3);
	}

	private static void EnsureIconPrefab(bool force = false)
	{
		var texture = LoadTexture(force);
		if (texture == _sIconTexture && _sIconPrefab != null)
			return;

		if (_sIconTexture != Plugin.FallbackTexture && _sIconTexture != texture)
			Destroy(_sIconTexture);
		_sIconTexture = texture;

		Destroy(_sIconPrefab?.GetComponent<SpriteRenderer>().sprite);
		Destroy(_sIconPrefab);
		_sIconPrefab = new GameObject("PlayerConsciousnessIcon");
		var sprRenderer = _sIconPrefab.AddComponent<SpriteRenderer>();
		sprRenderer.sortingOrder = 6001;
		sprRenderer.sprite = Sprite.Create(_sIconTexture, new Rect(0, 0, _sIconTexture.width, _sIconTexture.height), new Vector2(0.5f, 0.5f));
		_sIconPrefab.transform.SetParent(null);
		DontDestroyOnLoad(_sIconPrefab);
		_sIconPrefab.SetActive(false);

		foreach (var inst in _sInstances)
		{
			if (!inst || !inst.body) // don't reinit if it's about to be destroyed
				continue;
			inst.InitIcons();
		}
	}

	private static Texture2D LoadTexture(bool force = false)
	{
		if (Time.realtimeSinceStartup - _sLastCheckTime < 3f && !force)
			return _sIconTexture;
		_sLastCheckTime = Time.realtimeSinceStartup;

		var texturePath = "";
		try
		{
			texturePath = Path.Combine(Plugin.TextureDir, Plugin.ConfigIconFile.Value);

			if (!File.Exists(texturePath))
			{
				Plugin.Logger.LogWarning(
					$"Found no icon. Set {texturePath} as your icon.");
				ConsoleScript.instance.LogToConsole(
					$"<color=yellow>[{Plugin.ModName}] Found no icon. Set {texturePath} as your icon.</color>");
				File.WriteAllBytes(texturePath, Plugin.FallbackImage);
			}

			var writeTime = File.GetLastWriteTime(texturePath);
			if (writeTime == _sLastWriteTime)
				return _sIconTexture;
			_sLastWriteTime = writeTime;

			var bytes = File.ReadAllBytes(texturePath);
			if (bytes.Length < 2)
				return Plugin.FallbackTexture;

			var newTexture = new Texture2D(2, 2);
			bool success = newTexture.LoadImage(bytes);
			if (!success)
			{
				Destroy(newTexture);
				return Plugin.FallbackTexture;
			}
			newTexture.filterMode = FilterMode.Point;
			return newTexture;
		}
		catch (Exception ex)
		{
			Plugin.Logger.LogWarning($"Failed to load {texturePath}: " + ex.Message);
			ConsoleScript.instance.LogToConsole($"<color=yellow>[{Plugin.ModName}] Failed to load {texturePath}:\n\t" + ex.Message + "</color>");
			return Plugin.FallbackTexture;
		}
	}

	private void InitIcons()
	{
		Destroy(_icon1);
		Destroy(_icon2);
		Destroy(_icon3);
		var parentTransform = body.transform.parent.gameObject.transform;
		var color = _myColor;
		var scale = _myScale;
		_icon1 = Instantiate(_sIconPrefab, parentTransform, false);
		_icon1.transform.localScale = new Vector3(scale, scale, 0);
		_icon1.GetComponent<SpriteRenderer>().color = color;
		_icon2 = Instantiate(_sIconPrefab, parentTransform, false);
		_icon2.transform.localScale = new Vector3(scale + 0.5f, scale + 0.5f, 0);
		_icon2.GetComponent<SpriteRenderer>().color = color;
		_icon3 = Instantiate(_sIconPrefab, parentTransform, false);
		_icon3.transform.localScale = new Vector3(scale - 0.5f, scale - 0.5f, 0);
		_icon3.GetComponent<SpriteRenderer>().color = color;
	}
}
