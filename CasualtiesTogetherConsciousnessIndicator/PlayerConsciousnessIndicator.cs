using System;
using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CasualtiesTogetherConsciousnessIndicator;

internal class PlayerConsciousnessIndicator : MonoBehaviour
{
	public Body body;
	public object netPlayer;
	private GameObject _icon1;
	private GameObject _icon2;
	private GameObject _icon3;
	private Vector2 _pos;
	private float _floatInT = 0f;
	private float _floatOutT = 0f;
	private float _rotateT = Random.value;
	private bool _myDoRotate;
	private float _myScale;
	private Color _myColor;
	private GameObject _myIconPrefab;

	private static Texture2D _sIconTexture;
	private static GameObject _sIconPrefab;
	private static float _sLastCheckTime = 0f;
	private static DateTime _sLastWriteTime = DateTime.MinValue;

	private static readonly Vector3 Icon1Dir = Quaternion.Euler(0f, 0f, -15f) * Vector2.right * 1.5f;
	private static readonly Vector3 Icon1Axis = Quaternion.Euler(5f, 0f, 90f) * Icon1Dir;
	private static readonly Vector3 Icon2Dir = Quaternion.Euler(0f, 0f, -20f) * Vector2.right * 1.5f;
	private static readonly Vector3 Icon2Axis = Quaternion.Euler(5f, 0f, 90f) * Icon2Dir;
	private static readonly Vector3 Icon3Dir = Quaternion.Euler(0f, 0f, -10f) * Vector2.right * 1.5f;
	private static readonly Vector3 Icon3Axis = Quaternion.Euler(5f, 0f, 90f) * Icon3Dir;

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
			_myColor = Plugin.ConfigDoTint.Value ? GetPlayerColor() : Color.white;
			_myScale = Plugin.ConfigScale.Value;
			_myDoRotate = Plugin.ConfigDoRotate.Value;
			_pos = (Vector2)body.limbs[0].transform.position + Vector2.up * 10f;
			_myIconPrefab = _sIconPrefab;
			EnsureIcons();
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

		EnsureIcons();

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

		var headPos = (Vector2)body.limbs[0].transform.position;

		if (body.conscious)
		{
			_floatInT = 0f;
			_floatOutT += Time.deltaTime;
			var targetPos = headPos;
			targetPos.y += 5f;

			_pos.x = targetPos.x;
			_pos.y = Mathf.Lerp(_pos.y, targetPos.y, Time.deltaTime * 10f);

			if (targetPos.y - _pos.y is <= 1f or >= 8f || _floatOutT >= 2f)
			{
				_icon1.SetActive(false);
				_icon2.SetActive(false);
				_icon3.SetActive(false);
				return;
			}
		}
		else
		{
			_floatOutT = 0f;
			var targetPos = headPos;
			targetPos.y += 1.5f;

			if (!_icon1.activeSelf)
			{
				_floatInT = 0;
				_pos = headPos + Vector2.up * 4f;
				_icon1.SetActive(true);
				_icon2.SetActive(_myDoRotate);
				_icon3.SetActive(_myDoRotate);
			}

			if (_floatInT >= 2f)
			{
				_pos = targetPos;
			}
			else
			{
				_floatInT += Time.deltaTime;

				_pos.x = targetPos.x;
				_pos.y = Mathf.Lerp(_pos.y, targetPos.y, Time.deltaTime * 5f);
				if (Mathf.Abs(_pos.y - targetPos.y) is <= 0.25f or >= 8f)
					_floatInT = 2f;
			}
		}

		UpdateIcons();
	}

	private void UpdateIcons()
	{
		_rotateT += Time.deltaTime;
		if (_rotateT > 1)
			_rotateT %= 1;

		var pos = (Vector3)_pos;

		if (!Plugin.ConfigDoRotate.Value)
		{
			_icon1.transform.position = pos;
		}
		else
		{
			var angle = _rotateT * 360f;
			_icon1.transform.position = pos + Quaternion.AngleAxis(angle, Icon1Axis) * Icon1Dir;
			_icon2.transform.position = pos + Quaternion.AngleAxis(angle + 120f, Icon2Axis) * Icon2Dir;
			_icon3.transform.position = pos + Quaternion.AngleAxis(angle + 240f, Icon3Axis) * Icon3Dir;
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
		Destroy(_icon1);
		Destroy(_icon2);
		Destroy(_icon3);
	}

	private void EnsureIcons()
	{
		EnsureIconPrefab();

		if (_icon1 != null && _myIconPrefab == _sIconPrefab)
			return;

		_myIconPrefab = _sIconPrefab;
		InitIcons();
	}

	private static void EnsureIconPrefab()
	{
		var force = _sIconPrefab == null;

		if (Time.realtimeSinceStartup - _sLastCheckTime < 3f && !force)
			return;
		_sLastCheckTime = Time.realtimeSinceStartup;

		var texture = LoadTexture();
		if (texture == _sIconTexture && !force)
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
	}

	private static Texture2D LoadTexture()
	{
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
		var prefab = _myIconPrefab;
		_icon1 = Instantiate(prefab, parentTransform, false);
		_icon1.transform.localScale = new Vector3(scale, scale, 0);
		_icon1.GetComponent<SpriteRenderer>().color = color;
		_icon2 = Instantiate(prefab, parentTransform, false);
		_icon2.transform.localScale = new Vector3(scale + 0.5f, scale + 0.5f, 0);
		_icon2.GetComponent<SpriteRenderer>().color = color;
		_icon3 = Instantiate(prefab, parentTransform, false);
		_icon3.transform.localScale = new Vector3(scale - 0.5f, scale - 0.5f, 0);
		_icon3.GetComponent<SpriteRenderer>().color = color;
	}
}
