using Kingmaker.UI.Common;

namespace KingmakerSmartSorter
{
    internal enum ItemDiagnosticTab
    {
        Accessories,
        Usable,
        Notable,
        Miscellaneous
    }

    internal static class ItemDiagnosticTabInfo
    {
        internal static ItemsFilter.FilterType GetFilter(ItemDiagnosticTab tab)
        {
            switch (tab)
            {
                case ItemDiagnosticTab.Accessories:
                    return ItemsFilter.FilterType.Accessories;
                case ItemDiagnosticTab.Usable:
                    return ItemsFilter.FilterType.Usable;
                case ItemDiagnosticTab.Notable:
                    return ItemsFilter.FilterType.Notable;
                case ItemDiagnosticTab.Miscellaneous:
                    return ItemsFilter.FilterType.NonUsable;
                default:
                    return ItemsFilter.FilterType.NoFilter;
            }
        }

        internal static string GetFileName(ItemDiagnosticTab tab)
        {
            switch (tab)
            {
                case ItemDiagnosticTab.Accessories:
                    return "Items_Accessories.json";
                case ItemDiagnosticTab.Usable:
                    return "Items_Usable.json";
                case ItemDiagnosticTab.Notable:
                    return "Items_Notable.json";
                case ItemDiagnosticTab.Miscellaneous:
                    return "Items_Misc.json";
                default:
                    return "Items_Unknown.json";
            }
        }
    }
}
