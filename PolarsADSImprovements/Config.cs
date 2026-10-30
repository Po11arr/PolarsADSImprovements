using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using PolarsADSImprovements.Settings;
using PolarsADSImprovements.Settings.Elements;

namespace PolarsADSImprovements;

public class WeaponZoom
{
    [XmlAttribute] public string SubtypeId;
    [XmlAttribute] public float Magnification;
}

public class Config : INotifyPropertyChanged
{
    #region Options

    private bool enabled = true;
    private float coefficient = 1.33f;
    private float multiplier = 1f;
    private bool smoothZoom = true;
    private float zoomTime = 0.15f;
    private bool perWeaponZoom = true;
    private float defaultZoom = 2f;

    #endregion

    #region User interface

    public readonly string Title = "Polar's ADS Improvements";

    [Separator("Sensitivity")]

    [Checkbox(label: "FOV-based sensitivity", description: "Off = vanilla ironsight sensitivity (flat 0.6x)")]
    public bool Enabled
    {
        get => enabled;
        set => SetField(ref enabled, value);
    }

    [Slider(0f, 2f, 0.01f, SliderAttribute.SliderType.Float, label: "Monitor distance coefficient",
        description: "1.33 = CoD / Battlefield default\n0 = zoom ratio (slowest)\n1.78 = Battlefield 6")]
    public float Coefficient
    {
        get => coefficient;
        set => SetField(ref coefficient, value);
    }

    [Slider(0.1f, 2f, 0.01f, SliderAttribute.SliderType.Float, label: "ADS multiplier",
        description: "Flat multiplier on top of the FOV scaling")]
    public float Multiplier
    {
        get => multiplier;
        set => SetField(ref multiplier, value);
    }

    [Separator("Zoom")]

    [Checkbox(label: "Smooth zoom", description: "Eased zoom in and out. Off = vanilla linear 0.075 s")]
    public bool SmoothZoom
    {
        get => smoothZoom;
        set => SetField(ref smoothZoom, value);
    }

    [Slider(0.05f, 0.4f, 0.01f, SliderAttribute.SliderType.Float, label: "Zoom time (s)",
        description: "Time to fully zoom in or out with smooth zoom on")]
    public float ZoomTime
    {
        get => zoomTime;
        set => SetField(ref zoomTime, value);
    }

    [Checkbox(label: "Per-weapon zoom", description: "Off = vanilla fixed 40 degree zoom for every weapon")]
    public bool PerWeaponZoom
    {
        get => perWeaponZoom;
        set => SetField(ref perWeaponZoom, value);
    }

    [Slider(1f, 6f, 0.05f, SliderAttribute.SliderType.Float, label: "Default zoom (x)",
        description: "Magnification for weapons without their own setting, relative to your FOV")]
    public float DefaultZoom
    {
        get => defaultZoom;
        set => SetField(ref defaultZoom, value);
    }

    #endregion

    #region Per-weapon zoom

    // Edited through the dynamic per-weapon rows the SettingsGenerator adds below the options above.
    // Only values that differ from the weapon's preset or DefaultZoom are stored, so weapons the
    // player never changed keep following DefaultZoom.
    public List<WeaponZoom> WeaponZooms { get; set; } = new();

    // Config file format version. Null for files written by 1.1.0, which stored every weapon on dialog open.
    public int? ConfigVersion { get; set; }
    public const int CurrentConfigVersion = 2;

    private Dictionary<string, float> zoomLookup;

    /// <summary>Changes whenever a setting changes, so cached zoom values can be refreshed.</summary>
    [XmlIgnore]
    public int Revision { get; private set; }

    /// <summary>Magnification for a weapon: saved value, else preset, else DefaultZoom.</summary>
    public float GetZoom(string subtypeId, string displayName, bool isBaseGame)
    {
        if (zoomLookup == null)
        {
            zoomLookup = new Dictionary<string, float>(StringComparer.Ordinal);
            foreach (var entry in WeaponZooms)
                if (!string.IsNullOrEmpty(entry.SubtypeId) && entry.Magnification >= 1f)
                    zoomLookup[entry.SubtypeId] = entry.Magnification;
        }

        if (subtypeId != null && zoomLookup.TryGetValue(subtypeId, out var value))
            return value;

        return Fallback(displayName, isBaseGame);
    }

    public void SetZoom(string subtypeId, string displayName, bool isBaseGame, float magnification)
    {
        magnification = Math.Max(1f, magnification);
        var index = WeaponZooms.FindIndex(e => e.SubtypeId == subtypeId);

        if (Math.Abs(magnification - Fallback(displayName, isBaseGame)) < 0.001f)
        {
            // Same as the preset or default: forget it so the weapon keeps following DefaultZoom.
            if (index >= 0)
                WeaponZooms.RemoveAt(index);
        }
        else if (index >= 0)
        {
            WeaponZooms[index].Magnification = magnification;
        }
        else
        {
            WeaponZooms.Add(new WeaponZoom { SubtypeId = subtypeId, Magnification = magnification });
        }

        zoomLookup = null;
        Revision++;
    }

    private float Fallback(string displayName, bool isBaseGame)
    {
        return (isBaseGame ? Preset(displayName) : null) ?? defaultZoom;
    }

    // Starting values for vanilla weapons only, matched on model numbers in the display name.
    private static float? Preset(string displayName)
    {
        if (string.IsNullOrEmpty(displayName))
            return null;
        if (displayName.Contains("MR-8P")) return 3f;
        if (displayName.Contains("RO-1")) return 1.75f; // RO-1, PRO-1 launchers
        if (displayName.Contains("S-10") || displayName.Contains("S-20")) return 1.5f; // pistols
        return null;
    }

    /// <summary>Clamp values from a hand-edited or older config file.</summary>
    public void Validate()
    {
        coefficient = Clamp(coefficient, 0f, 2f, 1.33f);
        multiplier = Clamp(multiplier, 0.1f, 2f, 1f);
        zoomTime = Clamp(zoomTime, 0.05f, 0.4f, 0.15f);
        defaultZoom = Clamp(defaultZoom, 1f, 6f, 2f);
        WeaponZooms ??= new List<WeaponZoom>();

        // 1.1.0 saved every weapon's preset or default when the settings dialog opened.
        // Drop those copies so the weapons follow DefaultZoom again.
        if (ConfigVersion == null)
            WeaponZooms.RemoveAll(e => Math.Abs(e.Magnification - defaultZoom) < 0.001f
                                       || Math.Abs(e.Magnification - 3f) < 0.001f
                                       || Math.Abs(e.Magnification - 1.75f) < 0.001f
                                       || Math.Abs(e.Magnification - 1.5f) < 0.001f);
        ConfigVersion = CurrentConfigVersion;

        zoomLookup = null;
        Revision++;
    }

    private static float Clamp(float value, float min, float max, float fallback)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            return fallback;
        return Math.Max(min, Math.Min(max, value));
    }

    #endregion

    #region Property change notification boilerplate

    public static readonly Config Current = ConfigStorage.Load();

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        Revision++;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}
