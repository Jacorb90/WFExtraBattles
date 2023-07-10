using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace WFExtraBattles.Util
{
    public abstract class PluginData
    {
        protected static Dictionary<string, KeywordData> AllKeywords { get => AddressableLoader.groups["KeywordData"].lookup.ToStandard().WideCast<KeywordData>(); }
        protected static Dictionary<string, TraitData> AllTraits { get => AddressableLoader.groups["TraitData"].lookup.ToStandard().WideCast<TraitData>(); }
        protected static Dictionary<string, StatusEffectData> AllStatusEffects { get => AddressableLoader.groups["StatusEffectData"].lookup.ToStandard().WideCast<StatusEffectData>(); }
        protected static Dictionary<string, CardData> AllCards { get => AddressableLoader.groups["CardData"].lookup.ToStandard().WideCast<CardData>(); }
        protected static Dictionary<string, CardUpgradeData> AllCardUpgrades { get => AddressableLoader.groups["CardUpgradeData"].lookup.ToStandard().WideCast<CardUpgradeData>(); }
        protected static Dictionary<string, BattleData> AllBattles { get => AddressableLoader.groups["BattleData"].lookup.ToStandard().WideCast<BattleData>(); }
        protected static Dictionary<string, ClassData> AllClasses { get => AddressableLoader.groups["ClassData"].lookup.ToStandard().WideCast<ClassData>(); }

        private readonly CampaignPopModifier Modifier;

        public PluginData()
        {
            Modifier = new(AddressableLoader.groups["GameMode"].lookup["GameModeNormal"].Cast<GameMode>().populator);
        }

        /// <summary>
        /// Total setup function for plugin data; should include all data additions
        /// </summary>
        public abstract void Setup();

        /// <summary>
        /// Get keyword by name
        /// </summary>
        protected KeywordData GetKeyword(string key)
        {
            if (AllKeywords.ContainsKey(key)) return AllKeywords[key];

            WFExtraBattlesPlugin.Log.LogError($"Keyword {key} not present!");
            return null;
        }

        /// <summary>
        /// Get trait by name
        /// </summary>
        protected TraitData GetTrait(string key)
        {
            if (AllTraits.ContainsKey(key)) return AllTraits[key];

            WFExtraBattlesPlugin.Log.LogError($"Trait {key} not present!");
            return null;
        }

        /// <summary>
        /// Get status effect by name
        /// </summary>
        protected StatusEffectData GetStatusEffect(string key)
        {
            if (AllStatusEffects.ContainsKey(key)) return AllStatusEffects[key];

            WFExtraBattlesPlugin.Log.LogError($"Status Effect {key} not present!");
            return null;
        }

        /// <summary>
        /// Get card by name
        /// </summary>
        protected CardData GetCard(string key)
        {
            if (AllCards.ContainsKey(key)) return AllCards[key];
            if (AllCards.ContainsKey(Tables.Cards[key])) return AllCards[Tables.Cards[key]];

            WFExtraBattlesPlugin.Log.LogError($"Status Effect {key} not present!");
            return null;
        }

        /// <summary>
        /// Get card upgrade (charm) by name
        /// </summary>
        protected CardUpgradeData GetUpgrade(string key)
        {
            if (AllCardUpgrades.ContainsKey(key)) return AllCardUpgrades[key];

            WFExtraBattlesPlugin.Log.LogError($"Card upgrade {key} not present!");
            return null;
        }

        /// <summary>
        /// Get battle by name
        /// </summary>
        protected BattleData GetBattle(string key)
        {
            if (AllBattles.ContainsKey(key)) return AllBattles[key];

            WFExtraBattlesPlugin.Log.LogError($"Battle {key} not present!");
            return null;
        }
        /// <summary>
        /// Get battle by tier and id
        /// </summary>
        protected BattleData GetBattle(int tier, int id)
        {
            return Modifier.GetBattleFromPopulator(tier, id);
        }

        /// <summary>
        /// Get class (tribe) by name
        /// </summary>
        protected ClassData GetClass(string key)
        {
            if (AllClasses.ContainsKey(key)) return AllClasses[key];

            WFExtraBattlesPlugin.Log.LogError($"Class {key} not present!");
            return null;
        }

        /// <summary>
        /// Add given battle to the normal campaign at given tier, from 0 (first fight) to 8 (heart fight)
        /// </summary>
        protected void AddBattleToTier(string name, int tier)
        {
            Modifier.AddBattleToTier(tier, name);
        }
        /// <summary>
        /// Add given battle to the normal campaign at given tier, from 0 (first fight) to 8 (heart fight)
        /// </summary>
        protected void AddBattleToTier(BattleData data, int tier)
        {
            AddBattleToTier(data.name, tier);
        }

        /// <summary>
        /// Set only possible battle in tier, from 0 (first fight) to 8 (heart fight)
        /// </summary>
        protected void SetBattleInTier(string name, int tier)
        {
            Modifier.SetBattleInPopulator(tier, 0, name);
            Modifier.RemoveBattleFromTier(0, 1);
        }
        /// <summary>
        /// Set only possible battle in tier, from 0 (first fight) to 8 (heart fight)
        /// </summary>
        protected void SetBattleInTier(BattleData data, int tier)
        {
            SetBattleInTier(data.name, tier);
        }
    }
}
