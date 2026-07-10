using UnityModManagerNet;

namespace KingmakerSmartAutoBuff
{
    public sealed class Settings : UnityModManager.ModSettings
    {
        public ModLanguage Language = ModLanguage.Russian;
        public bool EnableMod = true;
        public bool LogDiagnostics = true;
        public bool OnlyOutOfCombat = true;
        public bool StopOnCombatStart = true;
        public float DelayBetweenCasts = 0.2f;
        public float CastTimeoutSeconds = 20f;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Normalize();
            Save(this, modEntry);
        }

        internal void Normalize()
        {
            if (!System.Enum.IsDefined(typeof(ModLanguage), Language))
            {
                Language = ModLanguage.Russian;
            }

            if (DelayBetweenCasts < 0f)
            {
                DelayBetweenCasts = 0f;
            }
            else if (DelayBetweenCasts > 3f)
            {
                DelayBetweenCasts = 3f;
            }

            if (CastTimeoutSeconds < 5f)
            {
                CastTimeoutSeconds = 5f;
            }
            else if (CastTimeoutSeconds > 60f)
            {
                CastTimeoutSeconds = 60f;
            }
        }
    }
}
