using System.Collections.Generic;
using Kingmaker.Items;
using Kingmaker.UI.Common;

namespace KingmakerSmartSorter
{
    internal static class SmartItemSorter
    {
        internal static List<ItemEntity> Sort(
            List<ItemEntity> items,
            ItemsFilter.FilterType filterType)
        {
            if (items == null || items.Count == 0)
            {
                return items == null ? new List<ItemEntity>() : new List<ItemEntity>(items);
            }

            List<ItemSortRecord> records = new List<ItemSortRecord>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                records.Add(ItemSortRecord.Create(items[i], i, filterType));
            }

            records.Sort(ItemSortRecordComparer.Instance);

            List<ItemEntity> result = new List<ItemEntity>(records.Count);
            for (int i = 0; i < records.Count; i++)
            {
                result.Add(records[i].Item);
            }

            SortDiagnostics.Report(records, filterType);
            return result;
        }
    }
}
