#if OVILLO_URP
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DesenredaElOvillo.Juego.Tres
{
    /// <summary>
    /// Acabado de maqueta con el postproceso de URP (solo se compila si el paquete URP está instalado), buscando
    /// el aire de foto macro de la hoja de Ovillo: desenfoque del fondo (miniatura), balance de blancos cálido,
    /// colores algo más saturados, bloom suave en los brillos de los ojos y la lámpara, y viñeta.
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

            var blancos = perfil.Add<WhiteBalance>(true);
            blancos.temperature.Override(10f);
            blancos.tint.Override(3f);

            var color = perfil.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.15f);
            color.contrast.Override(9f);
            color.saturation.Override(14f);
            color.colorFilter.Override(new Color(1f, 0.95f, 0.87f));

            var bloom = perfil.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.45f);
            bloom.scatter.Override(0.7f);

            var vineta = perfil.Add<Vignette>(true);
            vineta.intensity.Override(0.27f);
            vineta.smoothness.Override(0.5f);

            var profundidad = perfil.Add<DepthOfField>(true);
            profundidad.mode.Override(DepthOfFieldMode.Gaussian);
            profundidad.gaussianStart.Override(17f);
            profundidad.gaussianEnd.Override(34f);
            profundidad.gaussianMaxRadius.Override(1.4f);
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
