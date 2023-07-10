using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WFExtraBattles.Util
{
    internal class CampaignPopModifier
    {
        private readonly CampaignPopulator Pop;
        private readonly AddressableLoader.Group Battles;

        public CampaignPopModifier(CampaignPopulator pop)
        {
            Pop = pop;
            Battles = AddressableLoader.groups["BattleData"];
        }

        public BattleData GetBattleFromLoader(string name)
        {
            return Battles.lookup[name].Cast<BattleData>();
        }
        public BattleData GetBattleFromPopulator(int tier, int battle)
        {
            return Pop.tiers[tier].battlePool[battle];
        }

        public void SetBattleInPopulator(int tier, int battle, string name)
        {
            try
            {
                Pop.tiers[tier].battlePool[battle] = GetBattleFromLoader(name);
            } catch (Exception e)
            {
                WFExtraBattlesPlugin.Log.LogError(e.Message);
            }
        }

        public void AddBattleToTier(int tier, string name)
        {
            var list = Pop.tiers[tier].battlePool.ToList();
            list.Add(GetBattleFromLoader(name));
            Pop.tiers[tier].battlePool = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<BattleData>(list.ToArray());
        }

        public void RemoveBattleFromTier(int tier, int battle)
        {
            var list = Pop.tiers[tier].battlePool.ToList();
            list.RemoveAt(battle);
            Pop.tiers[tier].battlePool = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<BattleData>(list.ToArray());
        }

        public void SetBattleAsOnlyInTier(int tier, string name)
        {
            Pop.tiers[tier].battlePool = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<BattleData>(new BattleData[] { GetBattleFromLoader(name) });
        }
    }
}
