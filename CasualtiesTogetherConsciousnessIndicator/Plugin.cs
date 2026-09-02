using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KrokoshaCasualtiesMP;
using KrokoshaCasualtiesUtils;
using UnityEngine;

namespace CasualtiesTogetherConsciousnessIndicator;

[BepInPlugin(ModGUID, ModName, ModVersion)]
[BepInDependency("KrokoshaCasualtiesMP")]
public class Plugin : BaseUnityPlugin
{
	public const string ModGUID = "cump.consciousness.indicator";
	public const string ModName = "CasualtiesTogetherConsciousnessIndicator";
	public const string ModVersion = "1.0.0";

	internal new static ManualLogSource Logger;
	
	private readonly Harmony _harmony = new(ModGUID);

	public static Plugin Instance { get; private set; } = null!;
	
	public static ConfigEntry<bool> ConfigEnabled;
	public static ConfigEntry<string> ConfigIconFile;
	public static ConfigEntry<bool> ConfigDoRotate;

	public static string TextureDir;
	public static byte[] FallbackImage;
	public static Texture2D FallbackTexture;

	public void Awake()
	{
		Logger = base.Logger;
		Instance = this;
		
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
			"Which icon file within BepInEx/plugins/ConsciousnessIndicator to use");
		ConfigDoRotate = Config.Bind(
			"General",
			"DoRotate",
			false,
			"Set to true to create three rotating icons around an unconscious player");
		
		var assembly = Assembly.GetExecutingAssembly();
		const string assetName = "CasualtiesTogetherConsciousnessIndicator.assets.fallback.png";
		byte[] assetBytes;
		using (Stream manifestResourceStream = assembly.GetManifestResourceStream(assetName))
		{
			if (manifestResourceStream == null)
			{
				Logger.LogError($"Failed to load asset {assetName}!");
			}
			assetBytes = new byte[manifestResourceStream.Length];
			manifestResourceStream.Read(assetBytes, 0, assetBytes.Length);

			FallbackImage = assetBytes;
		}
		FallbackTexture = new Texture2D(2, 2);
		FallbackTexture.LoadImage(FallbackImage);
		FallbackTexture.filterMode = FilterMode.Point;
		
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
		if (!ConfigEnabled.Value || !KrokoshaScavMultiplayer.IsNetworkActiveAndIsWorldGenerated())
			return;

		foreach (var netBody in NetBody.all_instances)
		{
			if (!netBody.is_player || netBody.TryGetComponent<PlayerConsciousnessIcon>(out _))
				continue;
			netBody.gameObject.AddComponent<PlayerConsciousnessIcon>().nb = netBody;
		}
	}
}

internal class PlayerConsciousnessIcon : MonoBehaviour
{
	public NetBody nb;
	private GameObject _icon1;
	private GameObject _icon2;
	private GameObject _icon3;
	private Vector2 _pos;
	private float _t = 0f;
	private bool _myDoRotate;
	private static Texture2D _IconTexture;
	private static GameObject _IconPrefab;
	private static float _LastCheckTime = 0f;
	private static DateTime _LastWriteTime = DateTime.MinValue;
	private static List<PlayerConsciousnessIcon> _Instances = [];

	private void Start()
	{
		_Instances.Add(this);
		EnsureIconPrefab(true);
		InitIcons();
		_myDoRotate = Plugin.ConfigDoRotate.Value;
		_pos = (Vector2)nb.body.GetHead().transform.position + Vector2.up * 10f;
	}

	private void LateUpdate()
	{
		if (!nb || !nb.is_player || !Plugin.ConfigEnabled.Value)
		{
			Destroy(this);
			return;
		}

		EnsureIconPrefab();

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

		if (nb.body.conscious)
		{
			if (!_icon1.activeSelf)
				return;
			
			var headPos = nb.body.GetHead().transform.position;
			var position = _pos;
			var y = position.y;
			var leavePos = headPos + Vector3.up * 4f;
			position = Vector3.Lerp(position, leavePos, Time.deltaTime * 10f);
			position.y = Mathf.Lerp(y, leavePos.y, Time.deltaTime * 10f);
			_pos = position;
			
			_t += Time.deltaTime;
			if (_t > 1)
				_t = 0;

			if (!doRotate)
			{
				_icon1.transform.position = position;
			}
			else
			{
				var d = _t;
				{
					var v = Quaternion.Euler(0f, 0f, -15f) * Vector2.right * 1.5f;
					var v2 = Quaternion.Euler(0f, 0f, 90f) * v;
					var v3 = Quaternion.AngleAxis(Mathf.Lerp(0f, 360f, d), v2) * v;
					_icon1.transform.position = (Vector3)_pos + v3;
				}
				{
					var v = Quaternion.Euler(0f, 0f, -20f) * Vector2.right * 1.5f;
					var v2 = Quaternion.Euler(0f, 0f, 90f) * v;
					var v3 = Quaternion.AngleAxis(Mathf.Lerp(0f, 360f, d), v2) * v;
					_icon2.transform.position = (Vector3)_pos + Quaternion.AngleAxis(120f, v2) * v3;
				}
				{
					var v = Quaternion.Euler(0f, 0f, -10f) * Vector2.right * 1.5f;
					var v2 = Quaternion.Euler(0f, 0f, 90f) * v;
					var v3 = Quaternion.AngleAxis(Mathf.Lerp(0f, 360f, d), v2) * v;
					_icon3.transform.position = (Vector3)_pos + Quaternion.AngleAxis(240f, v2) * v3;
				}
			}
			
			if (Mathf.Abs(position.y - leavePos.y) <= 1f)
			{
				_icon1.SetActive(false);
				_icon2.SetActive(false);
				_icon3.SetActive(false);
			}
		}
		else
		{
			var headPos = nb.body.GetHead().transform.position;
			var position = _pos;
			var y = position.y;
			if (!_icon1.activeSelf)
			{
				position = headPos + Vector3.up * 4f;
				y = position.y;
				_icon1.SetActive(true);
				_icon2.SetActive(doRotate);
				_icon3.SetActive(doRotate);
			}
			var desired = headPos + Vector3.up * 1.5f;
			position = Vector3.Lerp(position, desired, Time.deltaTime * 5f);
			position.y = Mathf.Lerp(y, desired.y, Time.deltaTime * 5f);
			_pos = position;

			_t += Time.deltaTime;
			if (_t > 1)
				_t = 0;

			if (!doRotate)
			{
				_icon1.transform.position = _pos;
			}
			else
			{
				var d = _t;
				{
					var v = Quaternion.Euler(0, 0, -15f) * Vector2.right * 1.5f;
					var v2 = Quaternion.Euler(0, 0, 90f) * v;
					var v3 = Quaternion.AngleAxis(Mathf.Lerp(0, 360f, d), v2) * v;
					_icon1.transform.position = (Vector3)_pos + v3;
				}
				{
					var v = Quaternion.Euler(0, 0, -20f) * Vector2.right * 1.5f;
					var v2 = Quaternion.Euler(0, 0, 90f) * v;
					var v3 = Quaternion.AngleAxis(Mathf.Lerp(0, 360f, d), v2) * v;
					_icon2.transform.position = (Vector3)_pos + Quaternion.AngleAxis(120f, v2) * v3;
				}
				{
					var v = Quaternion.Euler(0, 0, -10f) * Vector2.right * 1.5f;
					var v2 = Quaternion.Euler(0, 0, 90f) * v;
					var v3 = Quaternion.AngleAxis(Mathf.Lerp(0, 360f, d), v2) * v;
					_icon3.transform.position = (Vector3)_pos + Quaternion.AngleAxis(240f, v2) * v3;
				}
			}
		}
	}

	private void OnDestroy()
	{
		_Instances.Remove(this);
		Destroy(_icon1);
		Destroy(_icon2);
		Destroy(_icon3);
	}
	
	private static void EnsureIconPrefab(bool force = false)
	{
		var texture = LoadTexture(force);
		if (texture == _IconTexture && _IconPrefab != null) 
			return;
		if (_IconTexture != Plugin.FallbackTexture)
			Destroy(_IconTexture);
		_IconTexture = texture;

		Destroy(_IconPrefab?.GetComponent<SpriteRenderer>().sprite);
		Destroy(_IconPrefab);
		_IconPrefab = new GameObject("PlayerConsciousnessIcon");
		var sprRenderer = _IconPrefab.AddComponent<SpriteRenderer>();
		sprRenderer.sortingOrder = 6001;
		sprRenderer.sprite = Sprite.Create(_IconTexture, new Rect(0, 0, _IconTexture.width, _IconTexture.height), new Vector2(0.5f, 0.5f));
		_IconPrefab.transform.SetParent(null);
		DontDestroyOnLoad(_IconPrefab);
		_IconPrefab.SetActive(false);

		foreach (var inst in _Instances)
			if (inst.nb) inst.InitIcons();
	}

	private static Texture2D LoadTexture(bool force = false)
	{
		if (Time.time - _LastCheckTime < 3f && !force)
			return _IconTexture;
		_LastCheckTime = Time.time;
		
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
			if (writeTime == _LastWriteTime)
				return _IconTexture;
			_LastWriteTime = writeTime;

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
		_icon1 = Instantiate(_IconPrefab, nb.body.transform, false);
		_icon1.transform.localScale = Vector3.one * 5f;
		_icon1.GetComponent<SpriteRenderer>().color = nb.player.plrcolor;
		_icon1.SetActive(false);
		_icon2 = Instantiate(_IconPrefab, nb.body.transform, false);
		_icon2.transform.localScale = Vector3.one * 5.5f;
		_icon2.GetComponent<SpriteRenderer>().color = nb.player.plrcolor;
		_icon2.SetActive(false);
		_icon3 = Instantiate(_IconPrefab, nb.body.transform, false);
		_icon3.transform.localScale = Vector3.one * 4.5f;
		_icon3.GetComponent<SpriteRenderer>().color = nb.player.plrcolor;
		_icon3.SetActive(false);
	}
}