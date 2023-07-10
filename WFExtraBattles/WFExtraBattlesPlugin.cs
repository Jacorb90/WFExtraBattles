using BepInEx;
using BepInEx.Unity.IL2CPP;
using System.Collections.Generic;
using System.Linq;
using System;
using BepInEx.Logging;
using Il2CppInterop.Runtime;
using Il2CppSystem.IO;
using HarmonyLib;
using System.Reflection;
using Il2CppSystem.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Collections;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Data;
using Il2CppSystem.Linq;
using UnityEngine.EventSystems;
using WFExtraBattles.Util;
using Newtonsoft.Json.Utilities;
using BepInEx.Unity.IL2CPP.Utils;
using Il2CppInterop.Runtime.Injection;
using WildfrostModMiya;
using static WFExtraBattles.Util.Extensions;
using static StatusEffectApplyX;

// CREDIT (base patches): https://github.com/Hopeless404/WildFrostHopeMods

namespace WFExtraBattles
{
    [BepInPlugin("Wildfrost.Jacorb.ExtraBattles", "ExtraBattles", "v0.0.1")]
    [BepInDependency("WildFrost.Miya.WildfrostAPI")]
    public class WFExtraBattlesPlugin : BasePlugin
    {
        internal static string ModsFolder = typeof(WFExtraBattlesPlugin).Assembly.Location.Replace("WFExtraBattles.dll", "");

        internal static new ManualLogSource Log;
        internal static Behaviour MainBehaviour;

        internal static List<Func<Entity, Entity, int, string, Action<Hit>>> eventsWhenHit = new List<Func<Entity, Entity, int, string, Action<Hit>>>();
        internal static List<Func<Entity, Entity, int, string, Action<Hit>>> eventsOnHit = new List<Func<Entity, Entity, int, string, Action<Hit>>>();
        internal static List<Func<Entity, Entity, int, string, Action<Hit>>> eventsOnHitPre = new List<Func<Entity, Entity, int, string, Action<Hit>>>();
        internal static List<Func<Entity, Action>> eventsTryDrag = new List<Func<Entity, Action>>();
        internal static List<Func<Entity, Action<Entity>>> eventsProcessUnits = new List<Func<Entity, Action<Entity>>>();
        internal static List<Func<Entity, Action<Entity>>> eventsOnPlace = new List<Func<Entity, Action<Entity>>>();
        internal static List<Func<StatusEffectApply, Action<StatusEffectApply>>> eventsOnStatusApplied = new List<Func<StatusEffectApply, Action<StatusEffectApply>>>();

        internal static BattleSetUp battleSetUp;
        // internal static BattleLogSystem battleLog;
        // internal static CardController cardController;
        internal static CardCharmDragHandler dragHandler;
        internal static DeckDisplaySequence deckDisplay;
        internal static GameMode gameMode;

        internal static List<ClassData> ClassDataAdditions = new List<ClassData>();
        internal static bool PopulatorHasUpdated = false;

        public class HopePatches : MonoBehaviour
        {
            #region CharacterSelect patches

            [HarmonyPatch(typeof(CharacterSelectScreen), "Start")]
            class Patch
            {
                static void Postfix(CharacterSelectScreen __instance)
                {
                    gameMode ??= AddressableLoader.groups["GameMode"].lookup["GameModeNormal"].Cast<GameMode>();

                    foreach (var classData in ClassDataAdditions)
                    {
                        if (gameMode.classes.ToList().Find(a => a.name == classData.name) == null)
                        {
                            //__instance.leaderSelection.options += 1;
                            //__instance.leaderSelection.differentTribes += 1;
                            __instance.options += 1;
                            __instance.differentTribes += 1;
                            gameMode.classes = gameMode.classes.ToArray().AddItem(classData).ToRefArray();
                        }
                    }
                }
            }






            [HarmonyPatch(typeof(Town), "Start")]
            class TownStart
            {
                static void Prefix()
                {
                    if (gameMode?.classes.Count > 3)
                        gameMode.classes = new Il2CppReferenceArray<ClassData>(gameMode.classes.RangeSubset(0, 3));
                }
            }
            #endregion


            #region Battle patches
            [HarmonyPatch(typeof(BattleSetUp), "Run")]
            class BattleSetUpRun
            {
                static void Postfix(BattleSetUp __instance)
                {

                    battleSetUp = __instance;
                    var battle = battleSetUp.battle;
                    Log.LogDebug("battle set up has run");
                    if (battle != null)
                    {
                        Log.LogDebug("non null battle");
                    }
                }
            }

            [HarmonyPatch(typeof(Battle), nameof(Battle.EntityCreated))]
            class BattleEntityCreated
            {
                static void Postfix(Entity entity, Battle __instance)
                {
                    var battle = __instance;
                    if (battle != null)
                        if (entity._data != null)
                        {
                            // Change name based on charms (disabled for now)
                            if (false /*entity.owner == battle.enemy && entity._data.upgrades.Count > 0*/)
                            {
                                var charmNames = string.Join(" ", entity._data.upgrades.ToArray().Select(charm => charm.titleKey.GetLocalizedString().Replace(" Charm", "").Adjectivise())).Replace("Crown".Adjectivise() + " ", "");
                                if (!entity._data.titleKey.GetLocalizedString().Contains(charmNames)) entity._data = entity._data.SetTitle($"{charmNames} {entity.data.titleKey.GetLocalizedString()}");
                            }

                            // Strengthen pulse animation when counter at 1
                            entity.imminentAnimation.strength = 0.5f;
                        }
                }
            }

            // these run during campaign generation
            //HarmonyPatch(typeof(ScriptUpgradeEnemies), "Run")]
            class ScriptUpgradeEnemiesRun
            {
                static void Postfix(ScriptUpgradeEnemies __instance)
                {
                    Log.LogDebug("upgrading enemies");
                }
            }

            //[HarmonyPatch(typeof(ScriptUpgradeEnemies), nameof(ScriptUpgradeEnemies.TryAddUpgrade), new Type[] {typeof(BattleWaveManager.WaveData), typeof(int)})]
            class ScriptUpgradeEnemiesTryAddUpgrade
            {
                static void Postfix(bool __result, ref BattleWaveManager.WaveData wave, int cardIndex)
                {
                    
                }
            }



            //[HarmonyPatch(typeof(BattleLogSystem), nameof(BattleLogSystem.LogHit))]
            class eventHit
            {
                static void Postfix(Entity attacker, Entity target, int damage, string damageType)
                {
                    foreach (var eventOnHit in eventsOnHit)
                    {
                        var action = eventOnHit(attacker, target, damage, damageType);
                        //action(attacker);
                        //action(target);
                    }
                    foreach (var eventWhenHit in eventsWhenHit)
                    {
                        var action = eventWhenHit(attacker, target, damage, damageType);
                        //action(attacker);
                        //action(target);
                    }
                }
            }

            // this seems to trigger during processunits, but not for things that normally don't have counter?
            // it actually doesn't work with tainted spike, maybe that's it
            //[HarmonyPatch(typeof(Events), nameof(Events.InvokeEntityHit))]
            [HarmonyPatch(typeof(BattleLogSystem), nameof(BattleLogSystem.Hit))]
            class eventHit2
            {
                static void Prefix(Hit hit)
                {
                    if (hit != null && hit.attacker != null)
                    {
                        Entity attacker = hit.attacker;
                        Entity target = hit.target;
                        int damage = hit.damage;
                        string damageType = hit.damageType;
                        foreach (var eventOnHit in eventsOnHitPre)
                        {
                            var action = eventOnHit(attacker, target, damage, damageType);
                            action(hit);
                        }
                    }
                }
                static void Postfix(Hit hit)
                {
                    if (hit.countsAsHit&& hit.attacker != null)
                    {
                        Entity attacker = hit.attacker;
                        Entity target = hit.target;
                        int damage = hit.damage;
                        string damageType = hit.damageType;
                        foreach (var eventOnHit in eventsOnHit)
                        {
                            var action = eventOnHit(attacker, target, damage, damageType);
                            action(hit);
                        }
                        foreach (var eventWhenHit in eventsWhenHit)
                        {
                            var action = eventWhenHit(attacker, target, damage, damageType);
                            action(hit);
                        }
                    }

                }
            }



            // simple patch methods break savecollection
            // by automatically converting SaveCollection<BattleWaveManager.WaveList> to SaveCollection<>
            //[HarmonyPatch(typeof(BattleGenerationScriptWaves), nameof(BattleGenerationScriptWaves.Run))]
            class TestPrefixingBattlesOnCreation
            {
                static void Postfix(ref BattleData battleData, int points)//, ref SaveCollection<T> __result)
                {
                    Log.LogDebug("postfix");
                }
            }


            //[HarmonyPatch(typeof(CardController), nameof(CardController.TryDrag))] //I would prefer to use this with CardControllerBattle but it doesn't recognise the method
            [HarmonyPatch(typeof(Events), nameof(Events.InvokeEntitySelect))]
            class TouchPress
            {
                static void Postfix(Entity entity)
                {
                    if (Battle.instance != null)
                    {
                        //cardController = Battle.instance.playerCardController;
                        var se = entity.statusEffects.Find(se => se.type == "sniper")?.Cast<StatusEffectBombard>();
                        if (se != null && entity.owner == Battle.instance.player)
                        {
                            var index = Battle.instance.allSlots.IndexOf((Func<CardSlot, bool>)(slot => slot == se.targetList[0]));
                            while (se.targetList[0] != Battle.instance.allSlots.ToList()[6 + ((index + 1) % 6)])
                                se.SetTargets().MoveNext(); // this causes the sound to fluctuate a bit
                                                            // if it's really a problem then figure out the eventref for targeting sfx

                            //se.targetList[0] = Battle.instance.allSlots.ToList()[(index+1) % 12];
                            // this is another method, but the visual indicator doesn't update
                            // if I can figure out how to update that, this is much cleaner
                        }
                    }
                }
            } // fun note: Battle.instance.rows[Battle.instance.enemy][0].Count gives the number of enemies in the first row



            [HarmonyPatch(typeof(Events), nameof(Events.InvokeEntityPlace))]
            class EntityPlace
            {
                static void Postfix(Entity entity, Il2CppReferenceArray<CardContainer> containers, bool freeMove)
                {
                    Log.LogDebug("Entity placed");
                    if (Battle.instance != null)
                    {
                        foreach (var eventOnPlace in eventsOnPlace)
                        {
                            var action = eventOnPlace(entity);
                            action(entity);
                        }
                    }
                }
            }


            [HarmonyPatch(typeof(BattleLogSystem), nameof(BattleLogSystem.StatusApplied))]
            class StatusApplied
            {
                static void Postfix(StatusEffectApply apply)
                {
                    Log.LogDebug("effect applied");
                    if (Battle.instance != null)
                    {
                        foreach (var eventOnStatusApplied in eventsOnStatusApplied)
                        {
                            var action = eventOnStatusApplied(apply);
                            action(apply);
                        }
                    }
                }
            }






            [HarmonyPatch(typeof(Battle), nameof(Battle.ProcessUnit))]
            class processUnit
            {
                static void Prefix(ref Entity unit)
                {
                    //im.Print(unit.name);
                    foreach (var eventProcessUnit in eventsProcessUnits)
                    {
                        var action = eventProcessUnit(unit);
                        action(unit);
                    }
                }
            }
            #endregion


            #region DeckPack patches
            [HarmonyPatch(typeof(DeckDisplaySequence), "Run")]
            class DeckDisplaySequenceRun
            {
                static void Postfix(DeckDisplaySequence __instance)
                {
                    deckDisplay = __instance;
                }
            }


            // Charm preview
            [HarmonyPatch(typeof(CardCharmDragHandler), nameof(CardCharmDragHandler.EntityHover))]
            class UpgradeAssign
            {
                static void Postfix(Entity entity)
                {
                    dragHandler = deckDisplay.charmDragHandler;
                    //dragHandler.instantAssign = true;
                    if (dragHandler.dragging != null) Log.LogDebug("hovering over " + $"{entity.name}" + dragHandler.dragging.data.name);
                    //CoroutineManager.Start(dragHandler.Assign(dragHandler.dragging, entity));//.MoveNext();

                }
            }

            #endregion


            #region CardScript patches

            [HarmonyPatch(typeof(CardScriptAddRandomHealth), nameof(CardScriptAddRandomHealth.Run))]
            class CardScriptAddRandomHealthRun
            {
                static void Prefix(out int __state, CardData target)
                {
                    __state = target.hp;
                }
                static void Postfix(int __state, CardData target)
                {
                    Log.LogDebug($"Added {target.hp - __state} to {target.name}");
                }

            }


            #endregion
        }

        internal class Behaviour : MonoBehaviour
        {
            private void Start()
            {
                this.StartCoroutine(Run());

                this.StartCoroutine(LoadAllGroups());
            }

            internal System.Collections.IEnumerator Run()
            {
                yield return new WaitUntil((Func<bool>)(() =>
                     AddressableLoader.IsGroupLoaded("KeywordData") && AddressableLoader.IsGroupLoaded("CardData") && AddressableLoader.IsGroupLoaded("GameModifierData")
                ));

                var pluginData = new WFEBPluginData();
                pluginData.Setup();
            }

            internal System.Collections.IEnumerator LoadAllGroups()
            {
                StartCoroutine(AddressableLoader.LoadGroup("CardUpgradeData"));
                StartCoroutine(AddressableLoader.LoadGroup("KeywordData"));
                StartCoroutine(AddressableLoader.LoadGroup("TraitData"));
                StartCoroutine(AddressableLoader.LoadGroup("BattleData"));
                StartCoroutine(AddressableLoader.LoadGroup("StatusEffectData"));

                yield return new WaitUntil((Func<bool>)(() => SceneManager.IsLoaded("Town")));
                StartCoroutine(AddressableLoader.LoadGroup("CardData"));
                StartCoroutine(AddressableLoader.LoadGroup("GameModifierData"));
            }
        }

        public unsafe override void Load()
        {
            Log = base.Log;
            ClassInjector.RegisterTypeInIl2Cpp<Behaviour>();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), "Wildfrost.Jacorb.ExtraBattles");

            MainBehaviour = AddComponent<Behaviour>();

            // Plugin startup logic
            Log.LogInfo("Extra Battles loaded!");
        }
    }
}
