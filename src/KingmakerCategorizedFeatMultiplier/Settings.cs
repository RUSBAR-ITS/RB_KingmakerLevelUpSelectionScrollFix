using System.Collections.Generic;
using UnityEngine;
using UnityModManagerNet;

namespace KingmakerCategorizedFeatMultiplier
{
    public sealed class Settings : UnityModManager.ModSettings
    {
        private const int CurrentSchemaVersion = 3;
        private const int MinMultiplier = 1;
        private const int MaxMultiplier = 999;

        private static readonly FeatureSelectionCategory[] AllCategories =
        {
            FeatureSelectionCategory.GeneralFeat,
            FeatureSelectionCategory.CombatFeat,
            FeatureSelectionCategory.StyleFeat,
            FeatureSelectionCategory.TeamworkFeat,
            FeatureSelectionCategory.SpellcasterFeat,
            FeatureSelectionCategory.SpellSpecialization,
            FeatureSelectionCategory.Familiar,
            FeatureSelectionCategory.Bloodline,
            FeatureSelectionCategory.BloodlineFeat,
            FeatureSelectionCategory.ArcaneBond,
            FeatureSelectionCategory.BloodlineClassSkill,
            FeatureSelectionCategory.BloodlineSpecial,
            FeatureSelectionCategory.Deity,
            FeatureSelectionCategory.Domain,
            FeatureSelectionCategory.ChannelEnergy,
            FeatureSelectionCategory.RaceHeritage,
            FeatureSelectionCategory.RaceSpecial,
            FeatureSelectionCategory.AnimalCompanion,
            FeatureSelectionCategory.NatureOrHunterBond,
            FeatureSelectionCategory.FavoredEnemy,
            FeatureSelectionCategory.FavoredTerrain,
            FeatureSelectionCategory.RangerCombatStyle,
            FeatureSelectionCategory.ClassTalent,
            FeatureSelectionCategory.RagePower,
            FeatureSelectionCategory.Discovery,
            FeatureSelectionCategory.Mercy,
            FeatureSelectionCategory.MagusArcana,
            FeatureSelectionCategory.KiPower,
            FeatureSelectionCategory.WizardSchool,
            FeatureSelectionCategory.WeaponTraining,
            FeatureSelectionCategory.WeaponMastery,
            FeatureSelectionCategory.ChosenWeapon,
            FeatureSelectionCategory.WeaponSpecial,
            FeatureSelectionCategory.KineticistElement,
            FeatureSelectionCategory.KineticistBlast,
            FeatureSelectionCategory.KineticistInfusion,
            FeatureSelectionCategory.KineticistWildTalent,
            FeatureSelectionCategory.KineticistBonusFeat,
            FeatureSelectionCategory.SpellbookSelection,
            FeatureSelectionCategory.Other
        };

        private static readonly FeatureSelectionCategory[] GeneralAndRaceCategories =
        {
            FeatureSelectionCategory.GeneralFeat,
            FeatureSelectionCategory.RaceHeritage,
            FeatureSelectionCategory.RaceSpecial
        };

        private static readonly FeatureSelectionCategory[] MartialCategories =
        {
            FeatureSelectionCategory.CombatFeat,
            FeatureSelectionCategory.StyleFeat,
            FeatureSelectionCategory.TeamworkFeat,
            FeatureSelectionCategory.WeaponSpecial,
            FeatureSelectionCategory.ChosenWeapon,
            FeatureSelectionCategory.WeaponTraining,
            FeatureSelectionCategory.WeaponMastery
        };

        private static readonly FeatureSelectionCategory[] MagicCategories =
        {
            FeatureSelectionCategory.SpellcasterFeat,
            FeatureSelectionCategory.SpellSpecialization,
            FeatureSelectionCategory.Familiar,
            FeatureSelectionCategory.WizardSchool
        };

        private static readonly FeatureSelectionCategory[] BloodlineCategories =
        {
            FeatureSelectionCategory.Bloodline,
            FeatureSelectionCategory.BloodlineFeat,
            FeatureSelectionCategory.ArcaneBond,
            FeatureSelectionCategory.BloodlineClassSkill,
            FeatureSelectionCategory.BloodlineSpecial
        };

        private static readonly FeatureSelectionCategory[] DivineCategories =
        {
            FeatureSelectionCategory.Deity,
            FeatureSelectionCategory.Domain,
            FeatureSelectionCategory.ChannelEnergy
        };

        private static readonly FeatureSelectionCategory[] NatureAndRangerCategories =
        {
            FeatureSelectionCategory.AnimalCompanion,
            FeatureSelectionCategory.NatureOrHunterBond,
            FeatureSelectionCategory.FavoredEnemy,
            FeatureSelectionCategory.FavoredTerrain,
            FeatureSelectionCategory.RangerCombatStyle
        };

        private static readonly FeatureSelectionCategory[] ClassTalentCategories =
        {
            FeatureSelectionCategory.ClassTalent,
            FeatureSelectionCategory.RagePower,
            FeatureSelectionCategory.Discovery,
            FeatureSelectionCategory.Mercy,
            FeatureSelectionCategory.MagusArcana,
            FeatureSelectionCategory.KiPower
        };

        private static readonly FeatureSelectionCategory[] KineticistCategories =
        {
            FeatureSelectionCategory.KineticistElement,
            FeatureSelectionCategory.KineticistBlast,
            FeatureSelectionCategory.KineticistInfusion,
            FeatureSelectionCategory.KineticistWildTalent,
            FeatureSelectionCategory.KineticistBonusFeat
        };

        private static readonly FeatureSelectionCategory[] PrestigeCategories =
        {
            FeatureSelectionCategory.SpellbookSelection
        };

        private static readonly FeatureSelectionCategory[] OtherCategories =
        {
            FeatureSelectionCategory.Other
        };

        public int SettingsSchemaVersion = 0;
        public int SettingsGeneration = 1;

        public bool EnablePatch = true;
        public ModLanguage Language = ModLanguage.Auto;

        public int GeneralFeatMultiplier = 1;
        public int CombatFeatMultiplier = 1;
        public int StyleFeatMultiplier = 1;
        public int TeamworkFeatMultiplier = 1;
        public int SpellcasterFeatMultiplier = 1;
        public int SpellSpecializationMultiplier = 1;
        public int FamiliarMultiplier = 1;
        public int BloodlineMultiplier = 1;
        public int BloodlineFeatMultiplier = 1;
        public int ArcaneBondMultiplier = 1;
        public int BloodlineClassSkillMultiplier = 1;
        public int BloodlineSpecialMultiplier = 1;
        public int DeityMultiplier = 1;
        public int DomainMultiplier = 1;
        public int ChannelEnergyMultiplier = 1;
        public int RaceHeritageMultiplier = 1;
        public int RaceSpecialMultiplier = 1;
        public int AnimalCompanionMultiplier = 1;
        public int NatureOrHunterBondMultiplier = 1;
        public int FavoredEnemyMultiplier = 1;
        public int FavoredTerrainMultiplier = 1;
        public int RangerCombatStyleMultiplier = 1;
        public int ClassTalentMultiplier = 1;
        public int RagePowerMultiplier = 1;
        public int DiscoveryMultiplier = 1;
        public int MercyMultiplier = 1;
        public int MagusArcanaMultiplier = 1;
        public int KiPowerMultiplier = 1;
        public int WizardSchoolMultiplier = 1;
        public int WeaponTrainingMultiplier = 1;
        public int WeaponMasteryMultiplier = 1;
        public int ChosenWeaponMultiplier = 1;
        public int WeaponSpecialMultiplier = 1;
        public int KineticistElementMultiplier = 1;
        public int KineticistBlastMultiplier = 1;
        public int KineticistInfusionMultiplier = 1;
        public int KineticistWildTalentMultiplier = 1;
        public int KineticistBonusFeatMultiplier = 1;
        public int SpellbookSelectionMultiplier = 1;
        public int OtherSelectionMultiplier = 1;

        // Legacy fields from 0.2.x. They remain public so Unity Mod Manager can load old Settings.xml files.
        public int WizardFeatMultiplier = 1;
        public int ClassAndSpecialFeatureMultiplier = 1;
        public int RaceAndHeritageMultiplier = 1;
        public int DivineAndDomainMultiplier = 1;
        public int CompanionAndRangerMultiplier = 1;

        public bool WarnAboutBagOfTricks = true;
        public bool LogAddFeaturesCalls = false;
        public bool LogSelectionDetails = false;
        public bool LogLocalizedSelectionNames = true;
        public bool LogAllFeatures = false;
        public string DiagnosticSourceNameFilter = string.Empty;
        public string DiagnosticSelectionNameFilter = string.Empty;
        public int MaxDetailedSelectionLogs = 80;

        private readonly Dictionary<FeatureSelectionCategory, string> m_MultiplierTexts =
            new Dictionary<FeatureSelectionCategory, string>();

        private bool m_TextFieldsInitialized;
        private ModLanguage m_LastDrawnLanguage;
        private string m_MaxDetailedSelectionLogsText;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            MigrateIfNeeded();
            ApplyTextFields();
            Save(this, modEntry);
        }

        internal void Draw()
        {
            MigrateIfNeeded();
            Normalize();
            EnsureTextFieldsInitialized();
            ReloadLocalizationIfNeeded();

            GUILayout.BeginVertical("box");

            GUILayout.Label(ModLocalization.T("Settings.Title"));
            DrawLanguageSelector();
            EnablePatch = GUILayout.Toggle(EnablePatch, ModLocalization.T("Settings.EnablePatch"));
            WarnAboutBagOfTricks = GUILayout.Toggle(WarnAboutBagOfTricks, ModLocalization.T("Settings.WarnBagOfTricks"));

            GUILayout.EndVertical();

            DrawCategoryGroup("Settings.Group.GeneralAndRace", GeneralAndRaceCategories);
            DrawCategoryGroup("Settings.Group.Martial", MartialCategories);
            DrawCategoryGroup("Settings.Group.Magic", MagicCategories);
            DrawCategoryGroup("Settings.Group.Bloodline", BloodlineCategories);
            DrawCategoryGroup("Settings.Group.Divine", DivineCategories);
            DrawCategoryGroup("Settings.Group.NatureAndRanger", NatureAndRangerCategories);
            DrawCategoryGroup("Settings.Group.ClassTalents", ClassTalentCategories);
            DrawCategoryGroup("Settings.Group.Kineticist", KineticistCategories);
            DrawCategoryGroup("Settings.Group.Prestige", PrestigeCategories);
            DrawCategoryGroup("Settings.Group.Other", OtherCategories);

            GUILayout.BeginVertical("box");

            GUILayout.Label(ModLocalization.T("Settings.Diagnostics"));
            LogAddFeaturesCalls = GUILayout.Toggle(LogAddFeaturesCalls, ModLocalization.T("Diagnostics.LogAddFeaturesCalls"));
            LogSelectionDetails = GUILayout.Toggle(LogSelectionDetails, ModLocalization.T("Diagnostics.LogSelectionDetails"));
            if (LogSelectionDetails)
            {
                LogLocalizedSelectionNames = GUILayout.Toggle(LogLocalizedSelectionNames, ModLocalization.T("Diagnostics.LogLocalizedNames"));
                LogAllFeatures = GUILayout.Toggle(LogAllFeatures, ModLocalization.T("Diagnostics.LogAllFeatures"));
                DiagnosticSourceNameFilter = DrawTextField("Diagnostics.SourceFilter", DiagnosticSourceNameFilter, 260f);
                DiagnosticSelectionNameFilter = DrawTextField("Diagnostics.SelectionFilter", DiagnosticSelectionNameFilter, 260f);
                MaxDetailedSelectionLogs = DrawIntField("Diagnostics.MaxDetailedLogs", MaxDetailedSelectionLogs, ref m_MaxDetailedSelectionLogsText, 1, 5000);
            }

            GUILayout.EndVertical();
        }

        internal void MigrateIfNeeded()
        {
            if (SettingsSchemaVersion >= CurrentSchemaVersion)
            {
                return;
            }

            int oldClassAndSpecial = ClampMultiplier(ClassAndSpecialFeatureMultiplier);
            int oldRaceAndHeritage = ClampMultiplier(RaceAndHeritageMultiplier);
            int oldDivineAndDomain = ClampMultiplier(DivineAndDomainMultiplier);
            int oldCompanionAndRanger = ClampMultiplier(CompanionAndRangerMultiplier);
            int oldOther = ClampMultiplier(OtherSelectionMultiplier);

            SpellcasterFeatMultiplier = ClampMultiplier(WizardFeatMultiplier);
            SpellSpecializationMultiplier = oldOther;
            FamiliarMultiplier = oldClassAndSpecial;
            BloodlineFeatMultiplier = oldOther;
            BloodlineSpecialMultiplier = ClampMultiplier(BloodlineMultiplier);
            DeityMultiplier = oldDivineAndDomain;
            DomainMultiplier = oldDivineAndDomain;
            ChannelEnergyMultiplier = oldDivineAndDomain;
            RaceHeritageMultiplier = oldRaceAndHeritage;
            RaceSpecialMultiplier = oldRaceAndHeritage;
            AnimalCompanionMultiplier = oldCompanionAndRanger;
            NatureOrHunterBondMultiplier = oldCompanionAndRanger;
            FavoredEnemyMultiplier = oldCompanionAndRanger;
            FavoredTerrainMultiplier = oldCompanionAndRanger;
            RangerCombatStyleMultiplier = oldCompanionAndRanger;
            ClassTalentMultiplier = oldClassAndSpecial;
            RagePowerMultiplier = oldClassAndSpecial;
            DiscoveryMultiplier = oldClassAndSpecial;
            MercyMultiplier = oldClassAndSpecial;
            MagusArcanaMultiplier = oldClassAndSpecial;
            KiPowerMultiplier = oldClassAndSpecial;
            WizardSchoolMultiplier = oldClassAndSpecial;
            WeaponTrainingMultiplier = oldClassAndSpecial;
            WeaponMasteryMultiplier = oldClassAndSpecial;
            ChosenWeaponMultiplier = oldClassAndSpecial;
            WeaponSpecialMultiplier = oldClassAndSpecial;
            KineticistElementMultiplier = oldClassAndSpecial;
            KineticistBlastMultiplier = oldClassAndSpecial;
            KineticistInfusionMultiplier = oldClassAndSpecial;
            KineticistWildTalentMultiplier = oldClassAndSpecial;
            KineticistBonusFeatMultiplier = oldClassAndSpecial;
            SpellbookSelectionMultiplier = oldClassAndSpecial;

            SettingsSchemaVersion = CurrentSchemaVersion;
            BumpSettingsGeneration();
            Normalize();

            if (m_TextFieldsInitialized)
            {
                ResetTextFieldsFromValues();
            }

            Logger.Info("Settings migrated to schema " + SettingsSchemaVersion + ".");
        }

        internal void Normalize()
        {
            GeneralFeatMultiplier = ClampMultiplier(GeneralFeatMultiplier);
            CombatFeatMultiplier = ClampMultiplier(CombatFeatMultiplier);
            StyleFeatMultiplier = ClampMultiplier(StyleFeatMultiplier);
            TeamworkFeatMultiplier = ClampMultiplier(TeamworkFeatMultiplier);
            SpellcasterFeatMultiplier = ClampMultiplier(SpellcasterFeatMultiplier);
            SpellSpecializationMultiplier = ClampMultiplier(SpellSpecializationMultiplier);
            FamiliarMultiplier = ClampMultiplier(FamiliarMultiplier);
            BloodlineMultiplier = ClampMultiplier(BloodlineMultiplier);
            BloodlineFeatMultiplier = ClampMultiplier(BloodlineFeatMultiplier);
            ArcaneBondMultiplier = ClampMultiplier(ArcaneBondMultiplier);
            BloodlineClassSkillMultiplier = ClampMultiplier(BloodlineClassSkillMultiplier);
            BloodlineSpecialMultiplier = ClampMultiplier(BloodlineSpecialMultiplier);
            DeityMultiplier = ClampMultiplier(DeityMultiplier);
            DomainMultiplier = ClampMultiplier(DomainMultiplier);
            ChannelEnergyMultiplier = ClampMultiplier(ChannelEnergyMultiplier);
            RaceHeritageMultiplier = ClampMultiplier(RaceHeritageMultiplier);
            RaceSpecialMultiplier = ClampMultiplier(RaceSpecialMultiplier);
            AnimalCompanionMultiplier = ClampMultiplier(AnimalCompanionMultiplier);
            NatureOrHunterBondMultiplier = ClampMultiplier(NatureOrHunterBondMultiplier);
            FavoredEnemyMultiplier = ClampMultiplier(FavoredEnemyMultiplier);
            FavoredTerrainMultiplier = ClampMultiplier(FavoredTerrainMultiplier);
            RangerCombatStyleMultiplier = ClampMultiplier(RangerCombatStyleMultiplier);
            ClassTalentMultiplier = ClampMultiplier(ClassTalentMultiplier);
            RagePowerMultiplier = ClampMultiplier(RagePowerMultiplier);
            DiscoveryMultiplier = ClampMultiplier(DiscoveryMultiplier);
            MercyMultiplier = ClampMultiplier(MercyMultiplier);
            MagusArcanaMultiplier = ClampMultiplier(MagusArcanaMultiplier);
            KiPowerMultiplier = ClampMultiplier(KiPowerMultiplier);
            WizardSchoolMultiplier = ClampMultiplier(WizardSchoolMultiplier);
            WeaponTrainingMultiplier = ClampMultiplier(WeaponTrainingMultiplier);
            WeaponMasteryMultiplier = ClampMultiplier(WeaponMasteryMultiplier);
            ChosenWeaponMultiplier = ClampMultiplier(ChosenWeaponMultiplier);
            WeaponSpecialMultiplier = ClampMultiplier(WeaponSpecialMultiplier);
            KineticistElementMultiplier = ClampMultiplier(KineticistElementMultiplier);
            KineticistBlastMultiplier = ClampMultiplier(KineticistBlastMultiplier);
            KineticistInfusionMultiplier = ClampMultiplier(KineticistInfusionMultiplier);
            KineticistWildTalentMultiplier = ClampMultiplier(KineticistWildTalentMultiplier);
            KineticistBonusFeatMultiplier = ClampMultiplier(KineticistBonusFeatMultiplier);
            SpellbookSelectionMultiplier = ClampMultiplier(SpellbookSelectionMultiplier);
            OtherSelectionMultiplier = ClampMultiplier(OtherSelectionMultiplier);

            MaxDetailedSelectionLogs = Mathf.Clamp(MaxDetailedSelectionLogs, 1, 5000);
            DiagnosticSourceNameFilter = DiagnosticSourceNameFilter ?? string.Empty;
            DiagnosticSelectionNameFilter = DiagnosticSelectionNameFilter ?? string.Empty;
        }

        internal int GetMultiplier(FeatureSelectionCategory category)
        {
            switch (category)
            {
                case FeatureSelectionCategory.GeneralFeat:
                    return ClampMultiplier(GeneralFeatMultiplier);
                case FeatureSelectionCategory.CombatFeat:
                    return ClampMultiplier(CombatFeatMultiplier);
                case FeatureSelectionCategory.StyleFeat:
                    return ClampMultiplier(StyleFeatMultiplier);
                case FeatureSelectionCategory.TeamworkFeat:
                    return ClampMultiplier(TeamworkFeatMultiplier);
                case FeatureSelectionCategory.SpellcasterFeat:
                    return ClampMultiplier(SpellcasterFeatMultiplier);
                case FeatureSelectionCategory.SpellSpecialization:
                    return ClampMultiplier(SpellSpecializationMultiplier);
                case FeatureSelectionCategory.Familiar:
                    return ClampMultiplier(FamiliarMultiplier);
                case FeatureSelectionCategory.Bloodline:
                    return ClampMultiplier(BloodlineMultiplier);
                case FeatureSelectionCategory.BloodlineFeat:
                    return ClampMultiplier(BloodlineFeatMultiplier);
                case FeatureSelectionCategory.ArcaneBond:
                    return ClampMultiplier(ArcaneBondMultiplier);
                case FeatureSelectionCategory.BloodlineClassSkill:
                    return ClampMultiplier(BloodlineClassSkillMultiplier);
                case FeatureSelectionCategory.BloodlineSpecial:
                    return ClampMultiplier(BloodlineSpecialMultiplier);
                case FeatureSelectionCategory.Deity:
                    return ClampMultiplier(DeityMultiplier);
                case FeatureSelectionCategory.Domain:
                    return ClampMultiplier(DomainMultiplier);
                case FeatureSelectionCategory.ChannelEnergy:
                    return ClampMultiplier(ChannelEnergyMultiplier);
                case FeatureSelectionCategory.RaceHeritage:
                    return ClampMultiplier(RaceHeritageMultiplier);
                case FeatureSelectionCategory.RaceSpecial:
                    return ClampMultiplier(RaceSpecialMultiplier);
                case FeatureSelectionCategory.AnimalCompanion:
                    return ClampMultiplier(AnimalCompanionMultiplier);
                case FeatureSelectionCategory.NatureOrHunterBond:
                    return ClampMultiplier(NatureOrHunterBondMultiplier);
                case FeatureSelectionCategory.FavoredEnemy:
                    return ClampMultiplier(FavoredEnemyMultiplier);
                case FeatureSelectionCategory.FavoredTerrain:
                    return ClampMultiplier(FavoredTerrainMultiplier);
                case FeatureSelectionCategory.RangerCombatStyle:
                    return ClampMultiplier(RangerCombatStyleMultiplier);
                case FeatureSelectionCategory.ClassTalent:
                    return ClampMultiplier(ClassTalentMultiplier);
                case FeatureSelectionCategory.RagePower:
                    return ClampMultiplier(RagePowerMultiplier);
                case FeatureSelectionCategory.Discovery:
                    return ClampMultiplier(DiscoveryMultiplier);
                case FeatureSelectionCategory.Mercy:
                    return ClampMultiplier(MercyMultiplier);
                case FeatureSelectionCategory.MagusArcana:
                    return ClampMultiplier(MagusArcanaMultiplier);
                case FeatureSelectionCategory.KiPower:
                    return ClampMultiplier(KiPowerMultiplier);
                case FeatureSelectionCategory.WizardSchool:
                    return ClampMultiplier(WizardSchoolMultiplier);
                case FeatureSelectionCategory.WeaponTraining:
                    return ClampMultiplier(WeaponTrainingMultiplier);
                case FeatureSelectionCategory.WeaponMastery:
                    return ClampMultiplier(WeaponMasteryMultiplier);
                case FeatureSelectionCategory.ChosenWeapon:
                    return ClampMultiplier(ChosenWeaponMultiplier);
                case FeatureSelectionCategory.WeaponSpecial:
                    return ClampMultiplier(WeaponSpecialMultiplier);
                case FeatureSelectionCategory.KineticistElement:
                    return ClampMultiplier(KineticistElementMultiplier);
                case FeatureSelectionCategory.KineticistBlast:
                    return ClampMultiplier(KineticistBlastMultiplier);
                case FeatureSelectionCategory.KineticistInfusion:
                    return ClampMultiplier(KineticistInfusionMultiplier);
                case FeatureSelectionCategory.KineticistWildTalent:
                    return ClampMultiplier(KineticistWildTalentMultiplier);
                case FeatureSelectionCategory.KineticistBonusFeat:
                    return ClampMultiplier(KineticistBonusFeatMultiplier);
                case FeatureSelectionCategory.SpellbookSelection:
                    return ClampMultiplier(SpellbookSelectionMultiplier);
                default:
                    return ClampMultiplier(OtherSelectionMultiplier);
            }
        }

        private void DrawCategoryGroup(string titleKey, FeatureSelectionCategory[] categories)
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label(ModLocalization.T(titleKey));

            for (int i = 0; i < categories.Length; i++)
            {
                DrawCategoryField(categories[i]);
            }

            GUILayout.EndVertical();
        }

        private void DrawCategoryField(FeatureSelectionCategory category)
        {
            string text;
            if (!m_MultiplierTexts.TryGetValue(category, out text))
            {
                text = GetMultiplier(category).ToString();
            }

            int current = GetMultiplier(category);
            int next = DrawMultiplierField("Category." + category, current, ref text);
            m_MultiplierTexts[category] = text;

            if (next != current)
            {
                SetMultiplier(category, next);
                BumpSettingsGeneration();
            }
        }

        private void DrawLanguageSelector()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(ModLocalization.T("Settings.Language") + ":", GUILayout.Width(230f));

            string autoLabel = ModLocalization.T("Language.Auto") + " (" + ModLocalization.CurrentLocaleCode + ")";
            string[] labels =
            {
                autoLabel,
                ModLocalization.T("Language.English"),
                ModLocalization.T("Language.Russian")
            };

            int selected = Mathf.Clamp((int)Language, 0, labels.Length - 1);
            int next = GUILayout.SelectionGrid(selected, labels, labels.Length, GUILayout.Width(360f));
            if (next != selected)
            {
                Language = (ModLanguage)next;
                ModLocalization.Reload();
                m_LastDrawnLanguage = Language;
            }

            GUILayout.EndHorizontal();
        }

        private int DrawMultiplierField(string labelKey, int value, ref string text)
        {
            return DrawIntField(labelKey, value, ref text, MinMultiplier, MaxMultiplier);
        }

        private static int DrawIntField(string labelKey, int value, ref string text, int min, int max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(ModLocalization.T(labelKey) + ":", GUILayout.Width(420f));
            string nextText = GUILayout.TextField(text ?? value.ToString(), GUILayout.Width(80f));
            GUILayout.Label(ModLocalization.T("Settings.ActiveValue") + ": " + value, GUILayout.Width(120f));
            GUILayout.EndHorizontal();

            text = nextText;

            int parsed;
            if (TryParseInt(text, out parsed))
            {
                return Mathf.Clamp(parsed, min, max);
            }

            return Mathf.Clamp(value, min, max);
        }

        private static string DrawTextField(string labelKey, string value, float width)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(ModLocalization.T(labelKey) + ":", GUILayout.Width(340f));
            string next = GUILayout.TextField(value ?? string.Empty, GUILayout.Width(width));
            GUILayout.EndHorizontal();
            return next ?? string.Empty;
        }

        private void ApplyTextFields()
        {
            EnsureTextFieldsInitialized();

            bool changed = false;
            for (int i = 0; i < AllCategories.Length; i++)
            {
                FeatureSelectionCategory category = AllCategories[i];
                string text;
                if (!m_MultiplierTexts.TryGetValue(category, out text))
                {
                    text = GetMultiplier(category).ToString();
                }

                int current = GetMultiplier(category);
                int next = ParseOrCurrent(text, current, MinMultiplier, MaxMultiplier);
                if (next != current)
                {
                    SetMultiplier(category, next);
                    changed = true;
                }
            }

            MaxDetailedSelectionLogs = ParseOrCurrent(m_MaxDetailedSelectionLogsText, MaxDetailedSelectionLogs, 1, 5000);

            Normalize();
            if (changed)
            {
                BumpSettingsGeneration();
            }

            ResetTextFieldsFromValues();
        }

        private void EnsureTextFieldsInitialized()
        {
            if (m_TextFieldsInitialized)
            {
                return;
            }

            ResetTextFieldsFromValues();
            m_TextFieldsInitialized = true;
            m_LastDrawnLanguage = Language;
        }

        private void ResetTextFieldsFromValues()
        {
            m_MultiplierTexts.Clear();
            for (int i = 0; i < AllCategories.Length; i++)
            {
                FeatureSelectionCategory category = AllCategories[i];
                m_MultiplierTexts[category] = GetMultiplier(category).ToString();
            }

            m_MaxDetailedSelectionLogsText = MaxDetailedSelectionLogs.ToString();
        }

        private void ReloadLocalizationIfNeeded()
        {
            if (m_LastDrawnLanguage == Language)
            {
                return;
            }

            ModLocalization.Reload();
            m_LastDrawnLanguage = Language;
        }

        private void SetMultiplier(FeatureSelectionCategory category, int value)
        {
            value = ClampMultiplier(value);

            switch (category)
            {
                case FeatureSelectionCategory.GeneralFeat:
                    GeneralFeatMultiplier = value;
                    break;
                case FeatureSelectionCategory.CombatFeat:
                    CombatFeatMultiplier = value;
                    break;
                case FeatureSelectionCategory.StyleFeat:
                    StyleFeatMultiplier = value;
                    break;
                case FeatureSelectionCategory.TeamworkFeat:
                    TeamworkFeatMultiplier = value;
                    break;
                case FeatureSelectionCategory.SpellcasterFeat:
                    SpellcasterFeatMultiplier = value;
                    break;
                case FeatureSelectionCategory.SpellSpecialization:
                    SpellSpecializationMultiplier = value;
                    break;
                case FeatureSelectionCategory.Familiar:
                    FamiliarMultiplier = value;
                    break;
                case FeatureSelectionCategory.Bloodline:
                    BloodlineMultiplier = value;
                    break;
                case FeatureSelectionCategory.BloodlineFeat:
                    BloodlineFeatMultiplier = value;
                    break;
                case FeatureSelectionCategory.ArcaneBond:
                    ArcaneBondMultiplier = value;
                    break;
                case FeatureSelectionCategory.BloodlineClassSkill:
                    BloodlineClassSkillMultiplier = value;
                    break;
                case FeatureSelectionCategory.BloodlineSpecial:
                    BloodlineSpecialMultiplier = value;
                    break;
                case FeatureSelectionCategory.Deity:
                    DeityMultiplier = value;
                    break;
                case FeatureSelectionCategory.Domain:
                    DomainMultiplier = value;
                    break;
                case FeatureSelectionCategory.ChannelEnergy:
                    ChannelEnergyMultiplier = value;
                    break;
                case FeatureSelectionCategory.RaceHeritage:
                    RaceHeritageMultiplier = value;
                    break;
                case FeatureSelectionCategory.RaceSpecial:
                    RaceSpecialMultiplier = value;
                    break;
                case FeatureSelectionCategory.AnimalCompanion:
                    AnimalCompanionMultiplier = value;
                    break;
                case FeatureSelectionCategory.NatureOrHunterBond:
                    NatureOrHunterBondMultiplier = value;
                    break;
                case FeatureSelectionCategory.FavoredEnemy:
                    FavoredEnemyMultiplier = value;
                    break;
                case FeatureSelectionCategory.FavoredTerrain:
                    FavoredTerrainMultiplier = value;
                    break;
                case FeatureSelectionCategory.RangerCombatStyle:
                    RangerCombatStyleMultiplier = value;
                    break;
                case FeatureSelectionCategory.ClassTalent:
                    ClassTalentMultiplier = value;
                    break;
                case FeatureSelectionCategory.RagePower:
                    RagePowerMultiplier = value;
                    break;
                case FeatureSelectionCategory.Discovery:
                    DiscoveryMultiplier = value;
                    break;
                case FeatureSelectionCategory.Mercy:
                    MercyMultiplier = value;
                    break;
                case FeatureSelectionCategory.MagusArcana:
                    MagusArcanaMultiplier = value;
                    break;
                case FeatureSelectionCategory.KiPower:
                    KiPowerMultiplier = value;
                    break;
                case FeatureSelectionCategory.WizardSchool:
                    WizardSchoolMultiplier = value;
                    break;
                case FeatureSelectionCategory.WeaponTraining:
                    WeaponTrainingMultiplier = value;
                    break;
                case FeatureSelectionCategory.WeaponMastery:
                    WeaponMasteryMultiplier = value;
                    break;
                case FeatureSelectionCategory.ChosenWeapon:
                    ChosenWeaponMultiplier = value;
                    break;
                case FeatureSelectionCategory.WeaponSpecial:
                    WeaponSpecialMultiplier = value;
                    break;
                case FeatureSelectionCategory.KineticistElement:
                    KineticistElementMultiplier = value;
                    break;
                case FeatureSelectionCategory.KineticistBlast:
                    KineticistBlastMultiplier = value;
                    break;
                case FeatureSelectionCategory.KineticistInfusion:
                    KineticistInfusionMultiplier = value;
                    break;
                case FeatureSelectionCategory.KineticistWildTalent:
                    KineticistWildTalentMultiplier = value;
                    break;
                case FeatureSelectionCategory.KineticistBonusFeat:
                    KineticistBonusFeatMultiplier = value;
                    break;
                case FeatureSelectionCategory.SpellbookSelection:
                    SpellbookSelectionMultiplier = value;
                    break;
                default:
                    OtherSelectionMultiplier = value;
                    break;
            }
        }

        private void BumpSettingsGeneration()
        {
            SettingsGeneration = SettingsGeneration == int.MaxValue ? 1 : SettingsGeneration + 1;
            LevelUpHelperAddFeaturesPatch.NotifySettingsChanged();
        }

        private static int ParseOrCurrent(string text, int current, int min, int max)
        {
            int parsed;
            if (!TryParseInt(text, out parsed))
            {
                return Mathf.Clamp(current, min, max);
            }

            return Mathf.Clamp(parsed, min, max);
        }

        private static bool TryParseInt(string text, out int value)
        {
            return int.TryParse((text ?? string.Empty).Trim(), out value);
        }

        private static int ClampMultiplier(int value)
        {
            return Mathf.Clamp(value, MinMultiplier, MaxMultiplier);
        }
    }
}
