using System;

namespace PolarsADSImprovements;

/// <summary>
/// Set after an unexpected error in a patch: every patch then leaves the game's own behaviour
/// alone until the game restarts. Logs the cause once.
/// </summary>
internal static class Guard
{
    public static bool Disabled { get; private set; }

    public static void Disable(string reason, Exception e)
    {
        if (Disabled)
            return;
        Disabled = true;
        Log.Error(reason + ", ADS changes are off until the game restarts", e);
    }
}
