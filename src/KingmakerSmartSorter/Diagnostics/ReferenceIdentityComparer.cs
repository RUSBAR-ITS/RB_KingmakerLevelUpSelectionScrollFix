using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KingmakerSmartSorter
{
    internal sealed class ReferenceIdentityComparer : IEqualityComparer<object>
    {
        internal static readonly ReferenceIdentityComparer Instance =
            new ReferenceIdentityComparer();

        private ReferenceIdentityComparer()
        {
        }

        bool IEqualityComparer<object>.Equals(object left, object right)
        {
            return ReferenceEquals(left, right);
        }

        int IEqualityComparer<object>.GetHashCode(object value)
        {
            return value == null ? 0 : RuntimeHelpers.GetHashCode(value);
        }
    }
}
