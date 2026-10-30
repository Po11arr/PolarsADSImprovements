using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.Game.World;

namespace PolarsADSImprovements;

internal readonly struct WeaponInfo(string subtypeId, string displayName, bool isBaseGame)
{
    public readonly string SubtypeId = subtypeId;
    public readonly string DisplayName = displayName;
    public readonly bool IsBaseGame = isBaseGame;
}

internal static class WeaponCatalog
{
    /// <summary>
    /// Hand weapons that take ammo (rifles, pistols, launchers, modded guns), read from the loaded
    /// definitions so no SubtypeId is hardcoded. Tools have no magazines and are skipped.
    /// </summary>
    public static List<WeaponInfo> ZoomableWeapons()
    {
        var result = new List<WeaponInfo>();
        try
        {
            var definitions = MyDefinitionManager.Static;
            if (definitions == null)
                return result;

            foreach (var item in definitions.GetPhysicalItemDefinitions())
            {
                if (item is not MyWeaponItemDefinition weaponItem || !item.Public)
                    continue;
                if (!definitions.TryGetWeaponDefinition(weaponItem.WeaponDefinitionId, out var weapon))
                    continue;
                if (weapon.AmmoMagazinesId == null || weapon.AmmoMagazinesId.Length == 0)
                    continue;

                result.Add(new WeaponInfo(item.Id.SubtypeName, item.DisplayNameText ?? item.Id.SubtypeName, item.Context?.IsBaseGame ?? false));
            }
        }
        catch (Exception e)
        {
            Log.Error("Could not list weapons", e);
        }

        result.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.DisplayName, b.DisplayName));
        return result;
    }

    // Zoom for the weapon in hand, looked up once per weapon and settings change instead of
    // every zoomed frame (the lookup reads the localized display name).
    private static MyPhysicalItemDefinition cachedItem;
    private static int cachedRevision;
    private static float cachedZoom;

    /// <summary>Clears the cache. Runs on world unload (MySession.OnUnloading).</summary>
    public static void Reset()
    {
        cachedItem = null;
    }

    /// <summary>Magnification for the local character's current weapon.</summary>
    public static float CurrentWeaponZoom(Config config)
    {
        var item = MySession.Static?.LocalCharacter?.CurrentWeapon?.PhysicalItemDefinition;
        if (item == null)
            return config.DefaultZoom;

        if (!ReferenceEquals(item, cachedItem) || config.Revision != cachedRevision)
        {
            cachedZoom = config.GetZoom(item.Id.SubtypeName, item.DisplayNameText, item.Context?.IsBaseGame ?? false);
            cachedItem = item;
            cachedRevision = config.Revision;
        }
        return cachedZoom;
    }
}
