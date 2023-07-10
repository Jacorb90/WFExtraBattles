using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WFExtraBattles.Util;
using WildfrostModMiya;
using static StatusEffectApplyX;
using static WFExtraBattles.Util.Extensions;

namespace WFExtraBattles
{
    internal class WFEBPluginData : PluginData
    {
        public override void Setup()
        {
            SetupStatusEffects();
            SetupEnemies();
            SetupBattles();
        }

        public void SetupStatusEffects()
        {
            StatusEffectAdder.CreateStatusEffectData<StatusEffectInstantMultiple>("WFExtraBattles", "ReduceCounterAndIncreaseAttack").SetText("Count down <keyword=counter> and increase <keyword=attack> by <{a}>").ModifyFields(se =>
            {
                se.effects = new Il2CppReferenceArray<StatusEffectInstant>(new StatusEffectInstant[] { CardAdder.VanillaStatusEffects.ReduceCounter.StatusEffectData().Cast<StatusEffectInstant>(),
                                                                                                           CardAdder.VanillaStatusEffects.IncreaseAttack.StatusEffectData().Cast<StatusEffectInstant>() });
                se.targetConstraints = new Il2CppReferenceArray<TargetConstraint>(new TargetConstraint[] { CreateTargetConstraint<TargetConstraintDoesAttack>(), CreateTargetConstraint<TargetConstraintMaxCounterMoreThan>() });
                se.type = "";
                return se;
            }).RegisterInGroup();

            StatusEffectAdder.CreateStatusEffectData<StatusEffectApplyXWhenYAppliedTo>("WFExtraBattles", "InkSpeedPower").SetText("When anything receives <keyword=null>, count down <keyword=counter> and increase <keyword=attack> by <{a}>")
            .ModifyFields(se =>
            {
                se.whenAppliedType = "ink";
                se.whenAppliedToFlags = (StatusEffectApplyX.ApplyToFlags)(-1);
                se.effectToApply = "WFExtraBattles.ReduceCounterAndIncreaseAttack".StatusEffectData();
                se.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                return se;
            }).RegisterInGroup();

            StatusEffectAdder.CreateStatusEffectData<StatusEffectApplyXWhenYAppliedTo>("WFExtraBattles", "Inktrigger").SetText("Trigger a random ally when anything receives <keyword=null>").ModifyFields(se =>
            {
                se.whenAppliedType = "ink";
                se.whenAppliedToFlags = (StatusEffectApplyX.ApplyToFlags)(-1);
                se.effectToApply = CardAdder.VanillaStatusEffects.Trigger.StatusEffectData();
                se.applyToFlags = StatusEffectApplyX.ApplyToFlags.RandomAlly;
                se.applyConstraints = new Il2CppReferenceArray<TargetConstraint>(new TargetConstraint[] { CreateTargetConstraint<TargetConstraintDoesAttack>() });
                return se;
            }).RegisterInGroup();

            StatusEffectAdder.CreateStatusEffectData<StatusEffectApplyXOnTurn>("WFExtraBattles", "CrownInker").SetText("Apply <{a}> <keyword=null> to all <sprite name=crown>'d enemies").ModifyFields(se =>
            {
                se.effectToApply = CardAdder.VanillaStatusEffects.Null.StatusEffectData();
                se.applyToFlags = ApplyToFlags.Enemies;
                se.applyConstraints = new Il2CppReferenceArray<TargetConstraint>(new TargetConstraint[] { CreateTargetConstraint<TargetConstraintHasCrown>() });
                return se;
            }).RegisterInGroup();
        }

        public void SetupEnemies()
        {
            var voido = GetCard("Voido");
            var dregg = GetCard("Dregg");

            CardAdder.CreateCardData("WFExtraBattles", "VoidalOrb")
                            .SetTitle("Voidal Orb")
                            .SetIsUnit()
                            .SetCardType(CardAdder.VanillaCardTypes.Enemy)
                            .SetCanPlay(CardAdder.CanPlay.CanPlayOnEnemy | CardAdder.CanPlay.CanPlayOnBoard)
                            .SetSprites(LoadSprite(WFExtraBattlesPlugin.ModsFolder, "Images\\Cards\\VoidalOrb"), dregg.backgroundSprite)
                            .SetStats(4, 1, 2)
                            .SetBloodProfile(CardAdder.VanillaBloodProfiles.BloodProfileBlack)
                            .SetIdleAnimationProfile(CardAdder.VanillaCardAnimationProfiles.FloatSquishAnimationProfile)
                            .SetTraits(CardAdder.VanillaTraits.Barrage.TraitStack(1), CardAdder.VanillaTraits.Explode.TraitStack(4))
                            .SetAttackEffects("Null".StatusEffectStack(2))
                            .RegisterInGroup();

            CardAdder.CreateCardData("WFExtraBattles", "Inkraken")
                            .SetTitle("Inkraken")
                            .SetIsUnit()
                            .SetCardType(CardAdder.VanillaCardTypes.Enemy)
                            .SetCanPlay(CardAdder.CanPlay.CanPlayOnEnemy | CardAdder.CanPlay.CanPlayOnBoard)
                            .SetSprites(LoadSprite(WFExtraBattlesPlugin.ModsFolder, "Images\\Cards\\Inkraken"), voido.backgroundSprite)
                            .SetStats(32, 4, 8)
                            .SetBloodProfile(CardAdder.VanillaBloodProfiles.BloodProfileBlack)
                            .SetIdleAnimationProfile(CardAdder.VanillaCardAnimationProfiles.FloatSquishAnimationProfile)
                            .SetStartWithEffects("WFExtraBattles.InkSpeedPower".StatusEffectStack(2), CardAdder.VanillaStatusEffects.ImmuneToSnow.StatusEffectStack(2))
                            .RegisterInGroup();

            CardAdder.CreateCardData("WFExtraBattles", "Clumina")
                            .SetTitle("Clumina")
                            .SetIsUnit()
                            .SetCardType(CardAdder.VanillaCardTypes.Enemy)
                            .SetCanPlay(CardAdder.CanPlay.CanPlayOnEnemy | CardAdder.CanPlay.CanPlayOnBoard)
                            .SetSprites(LoadSprite(WFExtraBattlesPlugin.ModsFolder, "Images\\Cards\\Clumina"), voido.backgroundSprite)
                            .SetStats(null, null, 0)
                            .SetBloodProfile(CardAdder.VanillaBloodProfiles.BloodProfileHusk)
                            .SetIdleAnimationProfile(CardAdder.VanillaCardAnimationProfiles.ShakeAnimationProfile)
                            .SetTraits(CardAdder.VanillaTraits.Backline.TraitStack(1))
                            .SetStartWithEffects("WFExtraBattles.Inktrigger".StatusEffectStack(1), CardAdder.VanillaStatusEffects.Scrap.StatusEffectStack(2))
                            .RegisterInGroup();

            CardAdder.CreateCardData("WFExtraBattles", "GrandVortex")
                            .SetTitle("Grand Vortex")
                            .SetIsUnit()
                            .SetCardType(CardAdder.VanillaCardTypes.Miniboss)
                            .SetCanPlay(CardAdder.CanPlay.CanPlayOnEnemy | CardAdder.CanPlay.CanPlayOnBoard)
                            .SetSprites(LoadSprite(WFExtraBattlesPlugin.ModsFolder, "Images\\Cards\\GrandVortex"), voido.backgroundSprite)
                            .SetStats(44, 4, 4)
                            .SetBloodProfile(CardAdder.VanillaBloodProfiles.BloodProfileBlack)
                            .SetIdleAnimationProfile(CardAdder.VanillaCardAnimationProfiles.GiantAnimationProfile)
                            .SetStartWithEffects("WFExtraBattles.CrownInker".StatusEffectStack(1))
                            .RegisterInGroup();
        }

        public void SetupBattles()
        {
            var deepvoidBattle = CreateBattleData("WFExtraBattles", "Deepvoid", "Deepvoid", new BattleWavePoolData[]
                {
                    CreateBattleWavePoolData("WFExtraBattles", "Deepvoid", "Wave Pool 1",
                        Array.Empty<BattleWavePoolData.Wave>(),
                        new List<string>() { "WFExtraBattles.VoidalOrb", "Voido", "WFExtraBattles.VoidalOrb", "WFExtraBattles.Inkraken" },
                        1
                     ),
                    CreateBattleWavePoolData("WFExtraBattles", "Deepvoid", "Wave Pool 2",
                        Array.Empty<BattleWavePoolData.Wave>(),
                        new List<string>() { "WFExtraBattles.VoidalOrb", "WFExtraBattles.Clumina", "WFExtraBattles.Clumina" },
                        1
                     ),
                    CreateBattleWavePoolData("WFExtraBattles", "Deepvoid", "Wave Pool 3",
                        Array.Empty<BattleWavePoolData.Wave>(),
                        new List<string>() { "Voido", "WFExtraBattles.GrandVortex", "WFExtraBattles.VoidalOrb", "WFExtraBattles.VoidalOrb" },
                        1
                     )
                }, sprite: LoadSprite(WFExtraBattlesPlugin.ModsFolder, "Images\\Battles\\GrandVortex"), goldGivers: 0).RegisterInGroup();

            // SetBattleInTier(deepvoidBattle, 0); // for testing

            AddBattleToTier(deepvoidBattle, 6); // for proper placement
        }
    }
}
