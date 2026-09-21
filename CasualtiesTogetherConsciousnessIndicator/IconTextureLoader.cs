using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CasualtiesTogetherConsciousnessIndicator;

internal static class IconTextureLoader
{
    private static byte[] _fallbackImage;
    private static Texture2D _fallbackTexture;

    private static Texture2D _customTexture;
    private static string _lastPath;
    private static DateTime _lastWriteTime;

    public static Texture2D LoadTexture()
    {
        EnsureFallbackTexture();
        var texturePath = "";
        Texture2D replacement = null;
        try
        {
            texturePath = Path.Combine(Plugin.TextureDir, Plugin.ConfigIconFile.Value);
            if (!File.Exists(texturePath))
            {
                Plugin.PrintWarning($"Found no icon. Set {texturePath} as your icon.");
                File.WriteAllBytes(texturePath, _fallbackImage);
            }

            var writeTime = File.GetLastWriteTime(texturePath);
            if (texturePath == _lastPath && writeTime == _lastWriteTime)
                return _customTexture ? _customTexture : _fallbackTexture;

            replacement = DecodeTexture(File.ReadAllBytes(texturePath));
            _lastPath = texturePath;
            _lastWriteTime = writeTime;
        }
        catch (Exception ex)
        {
            _lastPath = null;
            Plugin.PrintWarning($"Failed to load {texturePath}:\n\t{ex.Message}");
        }

        Object.Destroy(_customTexture);
        _customTexture = replacement;
        return _customTexture ? _customTexture : _fallbackTexture;
    }

    private static Texture2D DecodeTexture(byte[] bytes)
    {
        if (bytes.Length < 2)
            return null;

        var texture = new Texture2D(2, 2);
        try
        {
            if (!texture.LoadImage(bytes))
                return null;
            texture.filterMode = FilterMode.Point;
            var result = texture;
            texture = null;
            return result;
        }
        finally
        {
            Object.Destroy(texture);
        }
    }

    private static void EnsureFallbackTexture()
    {
        if (_fallbackTexture != null)
            return;

        const string assetName = "CasualtiesTogetherConsciousnessIndicator.assets.fallback.png";
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(assetName);
            if (stream == null)
                throw new Exception("manifestResourceStream is null");

            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);

            _fallbackImage = buffer.ToArray();
            _fallbackTexture = DecodeTexture(_fallbackImage);
            if (_fallbackTexture == null)
                throw new Exception("Failed to decode fallback image");
        }
        catch (Exception ex)
        {
            _fallbackImage = Texture2D.whiteTexture.EncodeToPNG();
            _fallbackTexture = Texture2D.whiteTexture;
            Plugin.PrintError($"Failed to load asset {assetName}: " + ex);
        }
    }
}
