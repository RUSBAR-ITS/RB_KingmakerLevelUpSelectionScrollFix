using System.Collections.Generic;
using Kingmaker.EntitySystem.Entities;

namespace KingmakerSmartAutoBuff
{
    internal sealed class BuffApplicationResult
    {
        internal List<UnitEntityData> Covered = new List<UnitEntityData>();
        internal List<UnitEntityData> Missing = new List<UnitEntityData>();
        internal List<string> Unavailable = new List<string>();
    }
}
