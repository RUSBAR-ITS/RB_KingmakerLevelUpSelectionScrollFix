using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;

namespace KingmakerCategorizedFeatMultiplier
{
    internal static class FeatureSelectionClassifier
    {
        internal static FeatureSelectionCategory Classify(
            BlueprintFeatureSelection selection,
            BlueprintScriptableObject source,
            out FeatureGroup primaryGroup,
            out FeatureGroup secondaryGroup,
            out string evidence)
        {
            primaryGroup = FeatureGroup.None;
            secondaryGroup = FeatureGroup.None;
            evidence = "null selection";

            if (selection == null)
            {
                return FeatureSelectionCategory.Other;
            }

            primaryGroup = selection.GetGroup();
            secondaryGroup = selection.Group2;

            FeatureSelectionCategory knownCategory = ClassifyKnownSelection(selection, source, out evidence);
            if (knownCategory != FeatureSelectionCategory.Other)
            {
                return knownCategory;
            }

            FeatureSelectionCategory category = MapGroup(primaryGroup);
            if (category != FeatureSelectionCategory.Other)
            {
                evidence = "primary group " + primaryGroup;
                return category;
            }

            category = MapGroup(secondaryGroup);
            if (category != FeatureSelectionCategory.Other)
            {
                evidence = "secondary group " + secondaryGroup;
                return category;
            }

            category = ClassifyFromFeatureGroups(selection.AllFeatures, out evidence);
            return category;
        }

        private static FeatureSelectionCategory ClassifyKnownSelection(
            BlueprintFeatureSelection selection,
            BlueprintScriptableObject source,
            out string evidence)
        {
            string selectionName = selection.name ?? string.Empty;
            string localizedName = selection.Name ?? string.Empty;
            string sourceName = source != null ? source.name ?? string.Empty : string.Empty;
            string combined = selectionName + " " + sourceName + " " + localizedName;

            if (ContainsAny(selectionName, "BloodlineArcaneArcaneBondFeature", "ArcaneBond"))
            {
                evidence = "known arcane bond selection; source=" + sourceName + ", selection=" + selectionName;
                return FeatureSelectionCategory.ArcaneBond;
            }

            if (ContainsAny(selectionName, "BloodlineArcaneClassSkillSelection", "BloodlineClassSkill")
                || (ContainsAny(sourceName, "Bloodline") && ContainsAny(selectionName, "ClassSkill")))
            {
                evidence = "known bloodline class skill selection; source=" + sourceName + ", selection=" + selectionName;
                return FeatureSelectionCategory.BloodlineClassSkill;
            }

            if (ContainsAny(selectionName, "SpellSpecialization"))
            {
                evidence = "known spell specialization selection; selection=" + selectionName;
                return FeatureSelectionCategory.SpellSpecialization;
            }

            if (ContainsAny(selectionName, "SorcererFeatSelection", "BloodlineFeatSelection")
                || (ContainsAny(sourceName, "Bloodline") && ContainsAny(selectionName, "FeatSelection")))
            {
                evidence = "known bloodline feat selection; source=" + sourceName + ", selection=" + selectionName;
                return FeatureSelectionCategory.BloodlineFeat;
            }

            if (ContainsAny(
                    selectionName,
                    "BloodlineArcaneNewArcanaSelection",
                    "BloodlineArcaneSchoolPowerSelection",
                    "BloodOfDragonsSelection",
                    "DragonTypeSelection",
                    "ScaledFistDragonSelection"))
            {
                evidence = "known bloodline special selection; source=" + sourceName + ", selection=" + selectionName;
                return FeatureSelectionCategory.BloodlineSpecial;
            }

            if (ContainsAny(selectionName, "SorcererBloodlineSelection", "BloodlineSelection", "BloodlineRequisiteSelection"))
            {
                evidence = "known bloodline selection; source=" + sourceName + ", selection=" + selectionName;
                return FeatureSelectionCategory.Bloodline;
            }

            if (ContainsAny(selectionName, "RangerStyle"))
            {
                evidence = "known ranger combat style selection; selection=" + selectionName;
                return FeatureSelectionCategory.RangerCombatStyle;
            }

            if (ContainsAny(selectionName, "DeitySelection", "PaladinDeitySelection"))
            {
                evidence = "known deity selection; selection=" + selectionName;
                return FeatureSelectionCategory.Deity;
            }

            if (ContainsAny(selectionName, "DomainSelection", "DomainsSelection", "SecondDomainsSelection", "DruidDomain"))
            {
                evidence = "known domain selection; selection=" + selectionName;
                return FeatureSelectionCategory.Domain;
            }

            if (ContainsAny(selectionName, "ChannelEnergy"))
            {
                evidence = "known channel energy selection; selection=" + selectionName;
                return FeatureSelectionCategory.ChannelEnergy;
            }

            if (ContainsAny(selectionName, "AasimarHeritage", "TieflingHeritage", "HeritageSelection"))
            {
                evidence = "known race heritage selection; selection=" + selectionName;
                return FeatureSelectionCategory.RaceHeritage;
            }

            if (ContainsAny(sourceName, "Adaptability") || ContainsAny(selectionName, "Adaptability"))
            {
                evidence = "known racial special selection; source=" + sourceName + ", selection=" + selectionName;
                return FeatureSelectionCategory.RaceSpecial;
            }

            if (ContainsAny(selectionName, "AnimalCompanion", "CompanionSelection", "MadDogCompanion", "SylvanCompanion"))
            {
                evidence = "known animal companion selection; selection=" + selectionName;
                return FeatureSelectionCategory.AnimalCompanion;
            }

            if (ContainsAny(selectionName, "DruidBond", "RangerBond", "HuntersBond", "HunterBond"))
            {
                evidence = "known nature or hunter bond selection; selection=" + selectionName;
                return FeatureSelectionCategory.NatureOrHunterBond;
            }

            if (ContainsAny(selectionName, "FavoriteEnemy", "FavoredEnemy"))
            {
                evidence = "known favored enemy selection; selection=" + selectionName;
                return FeatureSelectionCategory.FavoredEnemy;
            }

            if (ContainsAny(selectionName, "FavoriteTerrain", "FavoredTerrain", "TerrainMastery"))
            {
                evidence = "known favored terrain selection; selection=" + selectionName;
                return FeatureSelectionCategory.FavoredTerrain;
            }

            if (ContainsAny(selectionName, "SwordSaintChosenWeapon"))
            {
                evidence = "known chosen weapon selection; selection=" + selectionName;
                return FeatureSelectionCategory.ChosenWeapon;
            }

            if (ContainsAny(selectionName, "WeaponMastery"))
            {
                evidence = "known weapon mastery selection; selection=" + selectionName;
                return FeatureSelectionCategory.WeaponMastery;
            }

            if (ContainsAny(selectionName, "WeaponTraining"))
            {
                evidence = "known weapon training selection; selection=" + selectionName;
                return FeatureSelectionCategory.WeaponTraining;
            }

            if (ContainsAny(
                    selectionName,
                    "WeaponFocus",
                    "WeaponSpecialization",
                    "ArmorFocus",
                    "ExoticWeaponProficiency",
                    "FinesseTraining"))
            {
                evidence = "known weapon or armor special selection; selection=" + selectionName;
                return FeatureSelectionCategory.WeaponSpecial;
            }

            if (ContainsAny(selectionName, "WildTalentBonusFeat"))
            {
                evidence = "known kineticist bonus feat selection; selection=" + selectionName;
                return FeatureSelectionCategory.KineticistBonusFeat;
            }

            if (ContainsAny(selectionName, "ElementalFocus", "ExpandedDefense"))
            {
                evidence = "known kineticist element selection; selection=" + selectionName;
                return FeatureSelectionCategory.KineticistElement;
            }

            if (ContainsAny(selectionName, "BlastSelection", "KineticBlast"))
            {
                evidence = "known kineticist blast selection; selection=" + selectionName;
                return FeatureSelectionCategory.KineticistBlast;
            }

            if (ContainsAny(selectionName, "InfusionSelection"))
            {
                evidence = "known kineticist infusion selection; selection=" + selectionName;
                return FeatureSelectionCategory.KineticistInfusion;
            }

            if (ContainsAny(selectionName, "WildTalent", "MetakinesisMaster"))
            {
                evidence = "known kineticist wild talent selection; selection=" + selectionName;
                return FeatureSelectionCategory.KineticistWildTalent;
            }

            if (ContainsAny(selectionName, "SpellbookSelection", "MysticTheurge", "ArcaneTricksterSpellbook", "EldritchKnightSpellbook", "DragonDiscipleSpellbook"))
            {
                evidence = "known spellbook or prestige selection; selection=" + selectionName;
                return FeatureSelectionCategory.SpellbookSelection;
            }

            if (ContainsAny(selectionName, "Familiar", "ElementalWhispers"))
            {
                evidence = "known familiar selection; selection=" + selectionName;
                return FeatureSelectionCategory.Familiar;
            }

            if (ContainsAny(selectionName, "SpecialistSchool", "OppositionSchool", "SchoolSelection"))
            {
                evidence = "known wizard school selection; selection=" + selectionName;
                return FeatureSelectionCategory.WizardSchool;
            }

            if (ContainsAny(selectionName, "RagePower"))
            {
                evidence = "known rage power selection; selection=" + selectionName;
                return FeatureSelectionCategory.RagePower;
            }

            if (ContainsAny(selectionName, "Discovery"))
            {
                evidence = "known discovery selection; selection=" + selectionName;
                return FeatureSelectionCategory.Discovery;
            }

            if (ContainsAny(selectionName, "Mercy"))
            {
                evidence = "known mercy selection; selection=" + selectionName;
                return FeatureSelectionCategory.Mercy;
            }

            if (ContainsAny(selectionName, "MagusArcana", "EldritchMagusArcana"))
            {
                evidence = "known magus arcana selection; selection=" + selectionName;
                return FeatureSelectionCategory.MagusArcana;
            }

            if (ContainsAny(selectionName, "KiPower"))
            {
                evidence = "known ki power selection; selection=" + selectionName;
                return FeatureSelectionCategory.KiPower;
            }

            if (ContainsAny(selectionName, "TalentSelection", "AdvancedTalentSelection", "ArcanistExploit", "CombatTrick"))
            {
                evidence = "known class talent selection; selection=" + selectionName;
                return FeatureSelectionCategory.ClassTalent;
            }

            if (ContainsAny(combined, "Class Skill") && ContainsAny(combined, "Bloodline"))
            {
                evidence = "localized bloodline class skill fallback; source=" + sourceName + ", selection=" + selectionName;
                return FeatureSelectionCategory.BloodlineClassSkill;
            }

            evidence = string.Empty;
            return FeatureSelectionCategory.Other;
        }

        private static FeatureSelectionCategory ClassifyFromFeatureGroups(BlueprintFeature[] features, out string evidence)
        {
            evidence = "no usable feature groups";

            if (features == null || features.Length == 0)
            {
                return FeatureSelectionCategory.Other;
            }

            Dictionary<FeatureSelectionCategory, int> counts = new Dictionary<FeatureSelectionCategory, int>();
            FeatureGroup firstUsefulGroup = FeatureGroup.None;

            for (int i = 0; i < features.Length; i++)
            {
                BlueprintFeature feature = features[i];
                if (feature == null || feature.Groups == null)
                {
                    continue;
                }

                for (int j = 0; j < feature.Groups.Length; j++)
                {
                    FeatureGroup group = feature.Groups[j];
                    FeatureSelectionCategory category = MapGroup(group);
                    if (category == FeatureSelectionCategory.Other)
                    {
                        continue;
                    }

                    if (firstUsefulGroup == FeatureGroup.None)
                    {
                        firstUsefulGroup = group;
                    }

                    int count;
                    counts.TryGetValue(category, out count);
                    counts[category] = count + 1;
                }
            }

            if (counts.Count == 0)
            {
                return FeatureSelectionCategory.Other;
            }

            FeatureSelectionCategory bestCategory = FeatureSelectionCategory.Other;
            int bestCount = -1;

            foreach (KeyValuePair<FeatureSelectionCategory, int> pair in counts)
            {
                if (pair.Value > bestCount)
                {
                    bestCategory = pair.Key;
                    bestCount = pair.Value;
                }
            }

            evidence = "majority AllFeatures groups; first=" + firstUsefulGroup + ", count=" + bestCount;
            return bestCategory;
        }

        private static FeatureSelectionCategory MapGroup(FeatureGroup group)
        {
            switch (group)
            {
                case FeatureGroup.Feat:
                    return FeatureSelectionCategory.GeneralFeat;
                case FeatureGroup.CombatFeat:
                    return FeatureSelectionCategory.CombatFeat;
                case FeatureGroup.StyleFeat:
                    return FeatureSelectionCategory.StyleFeat;
                case FeatureGroup.TeamworkFeat:
                    return FeatureSelectionCategory.TeamworkFeat;
                case FeatureGroup.WizardFeat:
                    return FeatureSelectionCategory.SpellcasterFeat;

                case FeatureGroup.Racial:
                    return FeatureSelectionCategory.RaceSpecial;
                case FeatureGroup.AasimarHeritage:
                case FeatureGroup.TieflingHeritage:
                    return FeatureSelectionCategory.RaceHeritage;

                case FeatureGroup.Domain:
                case FeatureGroup.DruidDomain:
                case FeatureGroup.BlightDruidDomain:
                case FeatureGroup.ClericSecondaryDomain:
                    return FeatureSelectionCategory.Domain;
                case FeatureGroup.Deities:
                    return FeatureSelectionCategory.Deity;
                case FeatureGroup.ChannelEnergy:
                    return FeatureSelectionCategory.ChannelEnergy;

                case FeatureGroup.BloodLine:
                case FeatureGroup.DraconicBloodlineSelection:
                    return FeatureSelectionCategory.Bloodline;

                case FeatureGroup.AnimalCompanion:
                    return FeatureSelectionCategory.AnimalCompanion;
                case FeatureGroup.RangerBond:
                case FeatureGroup.DruidBond:
                    return FeatureSelectionCategory.NatureOrHunterBond;
                case FeatureGroup.FavoriteEnemy:
                    return FeatureSelectionCategory.FavoredEnemy;
                case FeatureGroup.FavoriteTerrain:
                    return FeatureSelectionCategory.FavoredTerrain;
                case FeatureGroup.RangerStyle:
                    return FeatureSelectionCategory.RangerCombatStyle;

                case FeatureGroup.RagePower:
                    return FeatureSelectionCategory.RagePower;
                case FeatureGroup.RogueTalent:
                case FeatureGroup.DefensivePower:
                case FeatureGroup.Trait:
                case FeatureGroup.CreatureType:
                    return FeatureSelectionCategory.ClassTalent;
                case FeatureGroup.Discovery:
                case FeatureGroup.VivisectionistDiscovery:
                    return FeatureSelectionCategory.Discovery;
                case FeatureGroup.Mercy:
                    return FeatureSelectionCategory.Mercy;
                case FeatureGroup.MagusArcana:
                case FeatureGroup.EldritchScionArcana:
                    return FeatureSelectionCategory.MagusArcana;
                case FeatureGroup.KiPowers:
                case FeatureGroup.ScaledFistKiPowers:
                    return FeatureSelectionCategory.KiPower;
                case FeatureGroup.SpecialistSchool:
                case FeatureGroup.OppositionSchool:
                case FeatureGroup.ThassilonianSpellbook:
                    return FeatureSelectionCategory.WizardSchool;

                case FeatureGroup.WeaponTraining:
                    return FeatureSelectionCategory.WeaponTraining;
                case FeatureGroup.FinesseTraining:
                case FeatureGroup.ExoticWeaponProficiency:
                    return FeatureSelectionCategory.WeaponSpecial;

                case FeatureGroup.ArcaneTricksterSpellbook:
                case FeatureGroup.EldritchKnightSpellbook:
                case FeatureGroup.DragonDiscipleSpellbook:
                case FeatureGroup.MysticTheurgeArcaneSpellbook:
                case FeatureGroup.MysticTheurgeDivineSpellbook:
                    return FeatureSelectionCategory.SpellbookSelection;

                case FeatureGroup.KineticBlast:
                    return FeatureSelectionCategory.KineticistBlast;
                case FeatureGroup.KineticBlastInfusion:
                    return FeatureSelectionCategory.KineticistInfusion;
                case FeatureGroup.KineticWildTalent:
                    return FeatureSelectionCategory.KineticistWildTalent;
                case FeatureGroup.KineticElementalFocus:
                    return FeatureSelectionCategory.KineticistElement;

                default:
                    return FeatureSelectionCategory.Other;
            }
        }

        private static bool ContainsAny(string value, params string[] needles)
        {
            if (string.IsNullOrEmpty(value) || needles == null)
            {
                return false;
            }

            for (int i = 0; i < needles.Length; i++)
            {
                if (!string.IsNullOrEmpty(needles[i])
                    && value.IndexOf(needles[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
