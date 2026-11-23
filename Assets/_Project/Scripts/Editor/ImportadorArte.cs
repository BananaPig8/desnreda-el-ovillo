using UnityEditor;
using UnityEngine;

namespace DesenredaElOvillo.Editor
{
    /// <summary>
    /// Ajustes de importación automáticos para todo el arte del juego (Assets/_Project/Resources/Arte):
    /// sprite único, sin mipmaps, transparencia correcta y buena calidad. Así nadie tiene que acordarse
    /// de configurar cada imagen a mano.
    /// </summary>
    public sealed class ImportadorArte : AssetPostprocessor
    {
        private const string CarpetaArte = "Assets/_Project/Resources/Arte/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').StartsWith(CarpetaArte)) return;
            var importador = (TextureImporter)assetImporter;
            importador.textureType = TextureImporterType.Sprite;
            importador.spriteImportMode = SpriteImportMode.Single;
            importador.alphaIsTransparency = true;
            importador.mipmapEnabled = false;
            importador.filterMode = FilterMode.Bilinear;
            importador.wrapMode = TextureWrapMode.Clamp;
            importador.textureCompression = TextureImporterCompression.CompressedHQ;
            importador.maxTextureSize = 2048;
            importador.spritePixelsPerUnit = 128;
        }
    }
}
