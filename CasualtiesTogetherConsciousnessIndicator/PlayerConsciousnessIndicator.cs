using System;
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
    private float _animT;
    private AnimationType _myAnimationType;
    private float _myScale;
    private Color _myColor;
    private SpriteRenderer _renderer1;
    private SpriteRenderer _renderer2;
    private SpriteRenderer _renderer3;
    private IndicatorSettings _settings;
    private bool _wasSleeping;
    private float _wakeGraceUntil = float.NegativeInfinity;

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
            _icon1 = CreateIcon(out _renderer1);
            _icon2 = CreateIcon(out _renderer2);
            _icon3 = CreateIcon(out _renderer3);
            _settings = body.sleeping ? Plugin.Sleeping : Plugin.Unconscious;
            _wasSleeping = body.sleeping;
            Plugin.Unconscious.Icon.Changed += RefreshSprite;
            Plugin.Sleeping.Icon.Changed += RefreshSprite;
            RefreshSprite();
            UpdatePrefs(true);
            _pos = (Vector2)body.limbs[0].transform.position + Vector2.up * 10f;
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning("PlayerConsciousnessIcon couldn't Start(): " + ex);
            Destroy(this);
        }
    }

    private void LateUpdate()
    {
        if (!body || !Plugin.Instance)
        {
            Destroy(this);
            return;
        }

        if (!body.alive)
        {
            _wasSleeping = false;
            _wakeGraceUntil = float.NegativeInfinity;
            HideIcons();
            return;
        }

        if (_wasSleeping && !body.sleeping)
            _wakeGraceUntil = Time.time + 3f;
        _wasSleeping = body.sleeping;
        if (body.sleeping || body.conscious)
            _wakeGraceUntil = float.NegativeInfinity;

        var needsIndicator = body.sleeping || !body.conscious;
        // Only switch while an indicator is needed, so recovery can finish its exit animation.
        if (needsIndicator)
        {
            var settings = body.sleeping || Time.time < _wakeGraceUntil ? Plugin.Sleeping : Plugin.Unconscious;
            if (_settings != settings)
            {
                _settings = settings;
                HideIcons();
                RefreshSprite();
                UpdatePrefs(true);
            }
        }

        if (!_settings.Enabled.Value)
        {
            HideIcons();
            return;
        }
        if (!needsIndicator && !_icon1.activeSelf)
            return;

        UpdatePrefs();

        var headPos = (Vector2)body.limbs[0].transform.position;

        if (!needsIndicator)
        {
            _floatInT = 0f;
            _floatOutT += Time.deltaTime;
            var targetPos = headPos;
            targetPos.y += 5f;

            _pos.x = targetPos.x;
            _pos.y = Mathf.Lerp(_pos.y, targetPos.y, Time.deltaTime * 10f);

            if (targetPos.y - _pos.y is <= 1f or >= 8f || _floatOutT >= 2f)
            {
                HideIcons();
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
                _icon2.SetActive(_myAnimationType == AnimationType.RotateAround);
                _icon3.SetActive(_myAnimationType == AnimationType.RotateAround);
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
        _animT += Time.deltaTime;
        if (_animT > 1)
            _animT %= 1;

        var pos = (Vector3)_pos;

        if (_myAnimationType == AnimationType.None)
        {
            _icon1.transform.position = pos;
        }
        else if (_myAnimationType == AnimationType.RotateAround)
        {
            var angle = _animT * 360f;
            _icon1.transform.position = pos + Quaternion.AngleAxis(angle, Icon1Axis) * Icon1Dir;
            _icon2.transform.position = pos + Quaternion.AngleAxis(angle + 120f, Icon2Axis) * Icon2Dir;
            _icon3.transform.position = pos + Quaternion.AngleAxis(angle + 240f, Icon3Axis) * Icon3Dir;
        }
        else if (_myAnimationType == AnimationType.Jumping)
        {
            if (_floatInT < 2f || _floatOutT != 0f)
            {
                _icon1.transform.position = pos;
                _animT = 0f;
            }
            else
            {
                var height = Mathf.Sin(_animT * Mathf.PI) * 1.25f;
                _icon1.transform.position = pos + (Vector3)(Vector2.up * height);
            }
        }
    }

    private void UpdatePrefs(bool force = false)
    {
        var animationType = _settings.AnimationType.Value;
        if (_myAnimationType != animationType || force)
        {
            _myAnimationType = animationType;
            _animT = _myAnimationType == AnimationType.RotateAround ? Random.value : 0f;
            if (_icon1.activeSelf)
            {
                _icon2.SetActive(_myAnimationType == AnimationType.RotateAround);
                _icon3.SetActive(_myAnimationType == AnimationType.RotateAround);
            }
        }

        var scale = _settings.Scale.Value;
        if (!Mathf.Approximately(_myScale, scale) || force)
        {
            _myScale = scale;
            _icon1.transform.localScale = new Vector3(scale, scale, 0);
            _icon2.transform.localScale = new Vector3(scale + 0.5f, scale + 0.5f, 0);
            _icon3.transform.localScale = new Vector3(scale - 0.5f, scale - 0.5f, 0);
        }

        var color = _settings.DoTint.Value ? GetPlayerColor() : Color.white;
        if (_myColor != color || force)
        {
            _myColor = color;
            _renderer1.color = color;
            _renderer2.color = color;
            _renderer3.color = color;
        }
    }

    private void OnDestroy()
    {
        if (Plugin.Unconscious != null)
            Plugin.Unconscious.Icon.Changed -= RefreshSprite;
        if (Plugin.Sleeping != null)
            Plugin.Sleeping.Icon.Changed -= RefreshSprite;
        Destroy(_icon1);
        Destroy(_icon2);
        Destroy(_icon3);
    }

    private void RefreshSprite()
    {
        var sprite = _settings.Icon.Sprite;
        if (_renderer1) _renderer1.sprite = sprite;
        if (_renderer2) _renderer2.sprite = sprite;
        if (_renderer3) _renderer3.sprite = sprite;
    }

    private void HideIcons()
    {
        _icon1.SetActive(false);
        _icon2.SetActive(false);
        _icon3.SetActive(false);
        _floatInT = 0f;
        _floatOutT = 0f;
    }

    private GameObject CreateIcon(out SpriteRenderer renderer)
    {
        var icon = new GameObject("PlayerConsciousnessIcon");
        icon.transform.SetParent(body.transform.parent, false);
        renderer = icon.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 6001;
        icon.SetActive(false);
        return icon;
    }
}
