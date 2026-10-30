using System;
using HarmonyLib;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using VRage.Game.Utils;

namespace PolarsADSImprovements;

internal static class AimScaling
{
    private const float FallbackVanillaIronsight = 0.6f;
    private static float vanillaIronsight;
    private static float vanillaZoomFov;

    // Vanilla MyCharacter.MoveAndRotate multiplies ironsight rotation by ROTATION_SPEED_IRONSIGHTS.
    // Read lazily (first ironsight input) so MyCharacter's static constructor is never forced early.
    private static float VanillaIronsight
    {
        get
        {
            if (vanillaIronsight <= 0f)
                vanillaIronsight = ReadStaticFloat(typeof(MyCharacter), "ROTATION_SPEED_IRONSIGHTS", 0.01f, 10f, FallbackVanillaIronsight);
            return vanillaIronsight;
        }
    }

    /// <summary>Vanilla ironsight FOV in radians (MyCameraZoomProperties.FIELD_OF_VIEW_MIN, 40 degrees).</summary>
    public static float VanillaZoomFov
    {
        get
        {
            if (vanillaZoomFov <= 0f)
                vanillaZoomFov = ReadStaticFloat(typeof(MyCameraZoomProperties), "FIELD_OF_VIEW_MIN", 0.05f, 3f, (float)(40.0 * Math.PI / 180.0));
            return vanillaZoomFov;
        }
    }

    private static float ReadStaticFloat(Type type, string fieldName, float min, float max, float fallback)
    {
        try
        {
            var field = AccessTools.Field(type, fieldName);
            if (field != null && field.IsStatic && field.FieldType == typeof(float))
            {
                var value = (float)field.GetValue(null);
                if (value > min && value < max)
                    return value;
            }
        }
        catch (Exception e)
        {
            Log.Error("Could not read " + type.Name + "." + fieldName, e);
        }

        Log.Info(type.Name + "." + fieldName + " not found, assuming " + fallback);
        return fallback;
    }

    /// <summary>
    /// Factor applied to the raw rotation input so the final ironsight speed equals
    /// the monitor-distance multiplier instead of vanilla's flat value.
    /// Returns 1 (vanilla behaviour) if the camera is unavailable.
    /// </summary>
    public static float CorrectionFactor(Config config)
    {
        var camera = MySector.MainCamera;
        if (camera?.Zoom == null)
            return 1f;

        var hipFov = camera.FieldOfView;
        var currentFov = camera.Zoom.GetFOV();
        var target = ZoomMultiplier(hipFov, currentFov, config.Coefficient) * config.Multiplier;
        return target / VanillaIronsight;
    }

    /// <summary>
    /// Monitor distance (vertical) sensitivity multiplier between two vertical FOVs in radians.
    /// coefficient 0 = zoom ratio (focal length), 1.33 = CoD / Battlefield default.
    /// </summary>
    public static float ZoomMultiplier(float hipFov, float zoomFov, float coefficient)
    {
        if (!(hipFov > 0f) || !(zoomFov > 0f) || zoomFov >= hipFov)
            return 1f;

        var hip = Math.Tan(hipFov * 0.5);
        var zoom = Math.Tan(zoomFov * 0.5);

        if (coefficient <= 0.0001f)
            return (float)(zoom / hip);

        return (float)(Math.Atan(coefficient * zoom) / Math.Atan(coefficient * hip));
    }
}
