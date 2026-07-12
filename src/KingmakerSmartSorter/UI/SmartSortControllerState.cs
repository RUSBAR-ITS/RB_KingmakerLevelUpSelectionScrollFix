using TMPro;

namespace KingmakerSmartSorter
{
    internal sealed class SmartSortControllerState
    {
        internal TMP_Dropdown.OptionData OptionData;
        internal int OptionIndex = -1;
        internal bool IsActive;
        internal bool IsRestoring;
        internal bool CustomSelectionHandled;
    }
}
