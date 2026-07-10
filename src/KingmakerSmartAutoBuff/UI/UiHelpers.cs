using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KingmakerSmartAutoBuff
{
    internal static class UiHelpers
    {
        private static GUIStyle s_WrappedLabel;

        internal static GUIStyle WrappedLabel
        {
            get
            {
                if (s_WrappedLabel == null)
                {
                    s_WrappedLabel = new GUIStyle(GUI.skin.label);
                    s_WrappedLabel.wordWrap = true;
                }

                return s_WrappedLabel;
            }
        }

        internal static string Shorten(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value ?? string.Empty;
            }

            return value.Substring(0, Math.Max(0, maxLength - 3)) + "...";
        }

        internal static string ListOrNone(IList<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return ModLocalization.T("Common.None");
            }

            return string.Join(", ", values.ToArray());
        }
    }
}
