using UnityEngine;

namespace KingmakerSmartAutoBuff
{
    internal static class TargetSelectionPanel
    {
        internal static void Draw(SmartAutoBuffUi ui)
        {
            UiState state = ui.State;

            GUILayout.BeginVertical("box");
            GUILayout.Label(ModLocalization.T("Targets.Title"));

            SpellCatalogEntry entry = ui.CurrentSpellEntry();
            if (entry == null)
            {
                GUILayout.Label(ModLocalization.T("Targets.SelectSpell"));
                GUILayout.EndVertical();
                return;
            }

            if (entry.TargetKind == TargetKind.Unsupported)
            {
                GUILayout.Label(ModLocalization.T("Targets.Unsupported"));
                GUILayout.EndVertical();
                return;
            }

            if (entry.TargetKind == TargetKind.NoTarget)
            {
                GUILayout.Label(ModLocalization.T("Targets.NoTarget"));
            }
            else if (entry.TargetKind == TargetKind.Self)
            {
                GUILayout.Label(ModLocalization.T("Targets.FixedSelf") + ": " + entry.CasterName);
            }
            else
            {
                if (state.TargetOptions.Count == 0)
                {
                    GUILayout.Label(ModLocalization.T("Targets.NoneAvailable"));
                }

                GUILayout.BeginHorizontal();
                foreach (TargetOption target in state.TargetOptions)
                {
                    bool selected = state.SelectedTargetIds.Contains(target.Id);
                    bool nextSelected = GUILayout.Toggle(selected, target.Name, "Button", GUILayout.Width(UiLayout.TargetButtonWidth));
                    if (nextSelected && !selected)
                    {
                        state.SelectedTargetIds.Add(target.Id);
                    }
                    else if (!nextSelected && selected)
                    {
                        state.SelectedTargetIds.Remove(target.Id);
                    }
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            GUI.enabled = ui.CanAddSelectedSpell();
            if (GUILayout.Button(ModLocalization.T("Button.AddAction"), GUILayout.Width(UiLayout.WideButtonWidth)))
            {
                ui.AddSelectedSpellToQueue();
            }

            GUI.enabled = true;
            GUILayout.Label(ModLocalization.T("Targets.AddHint"));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
    }
}
