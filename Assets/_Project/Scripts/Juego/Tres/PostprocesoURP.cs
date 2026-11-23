#if OVILLO_URP
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DesenredaElOvillo.Juego.Tres
{
    /// <summary>
    /// Acabado de maqueta con el postproceso de URP (solo se compila si el paquete URP está instalado):
    /// desenfoque del fondo (sensación de miniatura), bloom suave para la lámpara, viñeta y gradación cálida.
    /// </summary>
    public static class PostprocesoURP
    {
        public static void Montar(Transform padre)
        {
            var go = new GameObject("Postproceso");
            go.transform.SetParent(padre, false);
            var volumen = go.AddComponent<Volume>();
            volumen.isGlobal = true;
            volumen.priority = 10;
            var perfil = ScriptableObject.CreateInstance<VolumeProfile>();
            volumen.sharedProfile = perfil;

            var tono = perfil.Add<Tonemapping>(true);
            tono.mode.Override(TonemappingMode.Neutral);

            var color = perfil.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.15f);
            color.contrast.Override(6f);
            color.saturation.Override(6f);
            color.colorFilter.Override(new Color(1f, 0.96f, 0.9f));

            var bloom = perfil.Add<Bloom>(true);
            bloom.threshold.Override(1.05f);
            bloom.intensity.Override(0.35f);
            bloom.scatter.Override(0.6f);

            var vineta = perfil.Add<Vignette>(true);
            vineta.intensity.Override(0.24f);
            vineta.smoothness.Override(0.45f);

            var profundidad = perfil.Add<DepthOfField>(true);
            profundidad.mode.Override(DepthOfFieldMode.Gaussian);
            profundidad.gaussianStart.Override(17f);
            profundidad.gaussianEnd.Override(34f);
            profundidad.gaussianMaxRadius.Override(1.1f);
        }

        public static void PrepararCamara(Camera camara)
        {
            var datos = camara.GetUniversalAdditionalCameraData();
            datos.renderPostProcessing = true;
            datos.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            datos.renderShadows = true;
        }
    }
}
#endif
