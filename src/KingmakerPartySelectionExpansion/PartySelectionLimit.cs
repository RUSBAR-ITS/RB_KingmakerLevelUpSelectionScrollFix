using UnityEngine;

namespace KingmakerPartySelectionExpansion
{
    internal static class PartySelectionLimit
    {
        internal const int VanillaPartySize = 6;

        internal static int GetMaxActivePartySize()
        {
            Settings settings = Main.Settings;
            if (settings == null || !settings.EnablePatch)
            {
                return VanillaPartySize;
            }

            return Mathf.Clamp(settings.MaxActivePartySize, VanillaPartySize, 30);
        }
    }
}
