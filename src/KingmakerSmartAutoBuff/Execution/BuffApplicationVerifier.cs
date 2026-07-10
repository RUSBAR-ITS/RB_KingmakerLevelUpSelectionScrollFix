using System.Collections.Generic;
using Kingmaker.EntitySystem.Entities;

namespace KingmakerSmartAutoBuff
{
    internal static class BuffApplicationVerifier
    {
        internal static BuffApplicationResult VerifyRecipients(
            SpellCatalogEntry entry,
            AbilityBuffProfile profile,
            List<UnitEntityData> recipients)
        {
            BuffApplicationResult result = new BuffApplicationResult();
            if (recipients == null)
            {
                return result;
            }

            foreach (UnitEntityData recipient in recipients)
            {
                if (recipient == null)
                {
                    result.Unavailable.Add("<missing>");
                    continue;
                }

                ActiveBuffInfo matched;
                if (ActiveBuffHelper.HasAnyProfileBuff(recipient, profile, entry, out matched))
                {
                    result.Covered.Add(recipient);
                }
                else
                {
                    result.Missing.Add(recipient);
                }
            }

            return result;
        }
    }
}
