using System;
using System.Reflection;
using HarmonyLib;
using PolarsADSImprovements.Settings;
using PolarsADSImprovements.Settings.Layouts;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using VRage.Plugins;

// Define assembly version when compiled by Pulsar
#if !LOCAL_BUILD
[assembly: AssemblyVersion("1.1.2.0")]
[assembly: AssemblyFileVersion("1.1.2.0")]
#endif

namespace PolarsADSImprovements;

// ReSharper disable once UnusedType.Global
public class Plugin : IPlugin
{
    public const string Name = "PolarsADSImprovements";
    private SettingsGenerator settingsGenerator;
    private Harmony harmony;

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public void Init(object gameInstance)
    {
        try
        {
            var config = Config.Current; // loads the config, or writes it with defaults on first run
            harmony = new Harmony(Name);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            MySession.OnUnloading += WeaponCatalog.Reset;
            Log.Info("Loaded " + Assembly.GetExecutingAssembly().GetName().Version + (config.Enabled ? "" : ", FOV-based sensitivity off in settings"));
        }
        catch (Exception e)
        {
            // A missing patch target (game update) must not leave half the patches in place.
            Guard.Disable("Failed to load", e);
            harmony?.UnpatchAll(Name);
        }
    }

    public void Dispose()
    {
        try
        {
            MySession.OnUnloading -= WeaponCatalog.Reset;
            harmony?.UnpatchAll(Name); // only this plugin's patches
            WeaponCatalog.Reset();
        }
        catch (Exception e)
        {
            Log.Error("Dispose failed", e);
        }
    }

    public void Update()
    {
    }

    // ReSharper disable once UnusedMember.Global
    public void OpenConfigDialog()
    {
        try
        {
            settingsGenerator ??= new SettingsGenerator();
            settingsGenerator.SetLayout<Simple>();
            MyGuiSandbox.AddScreen(settingsGenerator.Dialog);
        }
        catch (Exception e)
        {
            Log.Error("Could not open the settings", e);
        }
    }
}
