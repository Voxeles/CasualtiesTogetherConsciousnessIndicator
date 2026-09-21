using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CasualtiesTogetherConsciousnessIndicator;

internal sealed class IconTextureLoader : IDisposable
{
    private static byte[] _fallbackImage;
    private Texture2D _texture;
    private string _requestedFile;
    private string _loadedPath;
    private DateTime _lastWriteTime;
    private float _nextCheckTime;

    public Sprite Sprite { get; private set; }
    public event Action Changed;

    public void ReloadIfNeeded(string fileName)
    {
        if (fileName == _requestedFile && Time.realtimeSinceStartup < _nextCheckTime)
            return;
        _requestedFile = fileName;
        _nextCheckTime = Time.realtimeSinceStartup + 3f;

        var path = "";
        Texture2D texture = null;
        Sprite sprite;
        try
        {
            path = Path.Combine(Plugin.TextureDir, fileName);
            if (!File.Exists(path))
            {
                File.WriteAllBytes(path, GetFallbackImage());
                Plugin.PrintWarning($"Created a fallback icon at {path}. Replace it with your own image.");
            }

            var writeTime = File.GetLastWriteTimeUtc(path);
            if (path == _loadedPath && writeTime == _lastWriteTime)
                return;

            texture = DecodeTexture(File.ReadAllBytes(path));
            sprite = CreateSprite(texture);
            _loadedPath = path;
            _lastWriteTime = writeTime;
        }
        catch (Exception ex)
        {
            Object.Destroy(texture);
            Plugin.PrintWarning($"Failed to load {path}:\n\t{ex.Message}");
            if (Sprite != null)
                return;

            texture = DecodeTexture(GetFallbackImage());
            sprite = CreateSprite(texture);
        }

        Replace(texture, sprite);
    }

    private void Replace(Texture2D texture, Sprite sprite)
    {
        var oldSprite = Sprite;
        var oldTexture = _texture;
        Sprite = sprite;
        _texture = texture;

        Changed?.Invoke();

        Object.Destroy(oldSprite);
        Object.Destroy(oldTexture);
    }

    public void Dispose()
    {
        Replace(null, null);
        Changed = null;
    }

    private static Sprite CreateSprite(Texture2D texture)
    {
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    private static Texture2D DecodeTexture(byte[] bytes)
    {
        var texture = new Texture2D(2, 2);
        try
        {
            if (!texture.LoadImage(bytes))
                throw new InvalidDataException("The file is not a supported image.");
            texture.filterMode = FilterMode.Point;
            return texture;
        }
        catch
        {
            Object.Destroy(texture);
            throw;
        }
    }

    private static byte[] GetFallbackImage()
    {
        if (_fallbackImage != null)
            return _fallbackImage;

        const string assetName = "CasualtiesTogetherConsciousnessIndicator.assets.fallback.png";
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(assetName);
            if (stream == null)
                throw new FileNotFoundException("Embedded fallback image was not found.");
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            _fallbackImage = buffer.ToArray();
        }
        catch (Exception ex)
        {
            _fallbackImage = Texture2D.whiteTexture.EncodeToPNG();
            Plugin.PrintError($"Failed to load asset {assetName}: {ex.Message}");
        }

        return _fallbackImage;
    }
}
