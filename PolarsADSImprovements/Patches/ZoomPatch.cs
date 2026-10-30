using System;
using HarmonyLib;
using VRage.Game.Utils;

namespace PolarsADSImprovements.Patches;

// Vanilla: ironsight FOV lerps linearly from your FOV to a fixed 40 degrees over 0.075 s.
// This replaces it with a per-weapon magnification and an eased, configurable transition.
// Purely visual and local; AimScaling reads the resulting FOV, so sensitivity follows it.
[HarmonyPatch(typeof(MyCameraZoomProperties), nameof(MyCameraZoomProperties.Update))]
// ReSharper disable once UnusedType.Global
public static class ZoomPatch
{
    private static float vanillaZoomTime = -1f;

    [HarmonyPrefix]
    // ReSharper disable once UnusedMember.Global
    public static void Prefix(ref float ___ZoomTime, ref float ___m_currentZoomTime)
    {
        if (Guard.Disabled)
        {
            // Put the vanilla zoom time back so nothing is left half changed.
            if (vanillaZoomTime > 0f && ___ZoomTime > 0f && Math.Abs(___ZoomTime - vanillaZoomTime) > 1e-5f)
            {
                ___m_currentZoomTime *= vanillaZoomTime / ___ZoomTime;
                ___ZoomTime = vanillaZoomTime;
            }
            return;
        }
        try
        {
            if (vanillaZoomTime < 0f)
                vanillaZoomTime = ___ZoomTime;

            var config = Config.Current;
            var desired = config.SmoothZoom ? Math.Max(0.01f, config.ZoomTime) : vanillaZoomTime;
            if (Math.Abs(___ZoomTime - desired) < 1e-5f || ___ZoomTime <= 0f)
                return;

            // Keep progress through a transition in progress when the duration changes.
            ___m_currentZoomTime *= desired / ___ZoomTime;
            ___ZoomTime = desired;
        }
        catch (Exception e)
        {
            Guard.Disable("Zoom time patch failed", e);
        }
    }

    [HarmonyPostfix]
    // ReSharper disable once UnusedMember.Global
    public static void Postfix(MyCameraZoomProperties __instance, float ___m_zoomLevel, ref float ___m_FOV, MyCamera ___m_camera)
    {
        if (Guard.Disabled)
            return;
        try
        {
            // m_zoomLevel: 1 = not zoomed, 0 = fully zoomed
            if (___m_zoomLevel >= 1f || ___m_camera == null || !__instance.ApplyToFov)
                return;

            var config = Config.Current;
            if (!config.PerWeaponZoom && !config.SmoothZoom)
                return;

            var hipTan = Math.Tan(___m_camera.FieldOfView * 0.5);
            var targetTan = config.PerWeaponZoom
                ? hipTan / Math.Max(1f, WeaponCatalog.CurrentWeaponZoom(config))
                : Math.Tan(AimScaling.VanillaZoomFov * 0.5);
            if (targetTan > hipTan)
                targetTan = hipTan;

            var progress = 1.0 - Math.Max(0f, Math.Min(1f, ___m_zoomLevel));
            if (config.SmoothZoom)
                progress = progress * progress * (3.0 - 2.0 * progress); // smoothstep

            // Interpolate in tan space so on-screen magnification changes evenly.
            ___m_FOV = (float)(2.0 * Math.Atan(hipTan + (targetTan - hipTan) * progress));
        }
        catch (Exception e)
        {
            Guard.Disable("Zoom patch failed", e);
        }
    }
}
