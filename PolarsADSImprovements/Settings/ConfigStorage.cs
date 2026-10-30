using System;
using System.IO;
using System.Xml.Serialization;
using VRage.FileSystem;

namespace PolarsADSImprovements.Settings;

/// <summary>
/// Config in the game's Storage folder. Missing file: settings from the old AdsSensitivity.cfg
/// are carried over if present, otherwise defaults; either way the file is written. Unreadable
/// file: defaults are used and the file is left for the player to fix or delete. Unknown or
/// missing fields from other versions fall back to their defaults.
/// </summary>
public static class ConfigStorage
{
    private const string LegacyFileName = "AdsSensitivity.cfg"; // plugin name up to 1.1.1
    private static readonly string ConfigFileName = string.Concat(Plugin.Name, ".cfg");

    private static string StoragePath(string fileName) => Path.Combine(MyFileSystem.UserDataPath, "Storage", fileName);

    public static void Save(Config config)
    {
        try
        {
            var path = StoragePath(ConfigFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var text = File.CreateText(path))
                new XmlSerializer(typeof(Config)).Serialize(text, config);
        }
        catch (Exception e)
        {
            Log.Error("Could not save " + ConfigFileName, e);
        }
    }

    public static Config Load()
    {
        string path, legacyPath;
        try
        {
            path = StoragePath(ConfigFileName);
            legacyPath = StoragePath(LegacyFileName);
        }
        catch (Exception e)
        {
            Log.Error("Could not find the config folder, using defaults", e);
            return new Config();
        }

        if (File.Exists(path))
            return Read(path) ?? new Config();

        // First run under this name: carry the old settings over, then write the new file.
        var config = File.Exists(legacyPath) ? Read(legacyPath) : null;
        if (config != null)
            Log.Info("Settings carried over from " + LegacyFileName);
        config ??= new Config();
        Save(config);
        return config;
    }

    private static Config Read(string path)
    {
        try
        {
            using (var streamReader = File.OpenText(path))
            {
                var config = (Config)new XmlSerializer(typeof(Config)).Deserialize(streamReader);
                if (config == null)
                    return null;
                config.Validate();
                return config;
            }
        }
        catch (Exception e)
        {
            Log.Error("Could not read " + path + ", using defaults", e);
            return null;
        }
    }
}
