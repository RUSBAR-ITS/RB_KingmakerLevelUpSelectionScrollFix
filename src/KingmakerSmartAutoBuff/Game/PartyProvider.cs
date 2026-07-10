using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;

namespace KingmakerSmartAutoBuff
{
    internal static class PartyProvider
    {
        internal static List<UnitEntityData> GetActiveParty()
        {
            try
            {
                Game game = Game.Instance;
                if (game == null || game.Player == null || game.Player.Party == null)
                {
                    return new List<UnitEntityData>();
                }

                return game.Player.Party
                    .Where(unit => unit != null)
                    .ToList();
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to read active party.", ex);
                return new List<UnitEntityData>();
            }
        }

        internal static UnitEntityData FindPartyUnit(string id, string name)
        {
            foreach (UnitEntityData unit in GetActiveParty())
            {
                if (!string.IsNullOrEmpty(id) && string.Equals(GetUnitId(unit), id, StringComparison.Ordinal))
                {
                    return unit;
                }

                if (!string.IsNullOrEmpty(name) && string.Equals(SafeUnitName(unit), name, StringComparison.Ordinal))
                {
                    return unit;
                }
            }

            return null;
        }

        internal static string GetUnitId(UnitEntityData unit)
        {
            if (unit == null)
            {
                return string.Empty;
            }

            try
            {
                return !string.IsNullOrEmpty(unit.UniqueId) ? unit.UniqueId : SafeUnitName(unit);
            }
            catch
            {
                return SafeUnitName(unit);
            }
        }

        internal static string SafeUnitName(UnitEntityData unit)
        {
            try
            {
                return unit != null && !string.IsNullOrEmpty(unit.CharacterName) ? unit.CharacterName : "<unit>";
            }
            catch
            {
                return "<unit>";
            }
        }
    }
}
