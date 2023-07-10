using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.Localization;
using UnityEngine;
using WildfrostModMiya;
using Il2CppInterop.Runtime.InteropTypes;
using static JournalNameHistory;
using System.IO;

// CREDIT: https://github.com/Hopeless404/WildFrostHopeMods

namespace WFExtraBattles.Util
{
    public static class Extensions
    {
        #region SpriteExtensions
        public static Texture2D MakeReadable(this Texture2D texture)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(
                                    texture.width,
                                    texture.height,
                                    0,
                                    RenderTextureFormat.Default,
                                    RenderTextureReadWrite.Linear);

            Graphics.Blit(texture, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(texture.width, texture.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            return readableText;
        }
        #endregion

        #region CardExtensions
        public static CardData AddUpgrade(this CardData t, string name)
        {
            t.upgrades.Add(AddressableLoader.groups["CardUpgradeData"].lookup[name].Cast<CardUpgradeData>());
            return t;
        }

        public static CardUpgradeData AddTargetConstraint(this CardUpgradeData t, TargetConstraint tc)
        {
            t.targetConstraints ??= new Il2CppReferenceArray<TargetConstraint>(0);
            t.targetConstraints = new Il2CppReferenceArray<TargetConstraint>(t.targetConstraints.AddItem(tc).ToArray());
            return t;
        }
        public static CardUpgradeData AddScript(this CardUpgradeData t, CardScript cs)
        {
            t.scripts ??= new Il2CppReferenceArray<CardScript>(0);
            t.scripts = new Il2CppReferenceArray<CardScript>(t.scripts.AddItem(cs).ToArray());
            return t;
        }
        public static T AddTargetConstraint<T>(this T t, TargetConstraint tc) where T : StatusEffectData
        {
            t.targetConstraints = new Il2CppReferenceArray<TargetConstraint>(t.targetConstraints.AddItem(tc).ToArray());
            return t;
        }
        public static T AddApplyConstraint<T>(this T t, TargetConstraint tc) where T : StatusEffectApplyX
        {
            t.applyConstraints = new Il2CppReferenceArray<TargetConstraint>(t.applyConstraints.AddItem(tc).ToArray());
            return t;
        }
        public static CardData AddTargetConstraint(this CardData t, TargetConstraint tc)
        {
            t.targetConstraints = new Il2CppReferenceArray<TargetConstraint>(t.targetConstraints.AddItem(tc).ToArray());
            return t;
        }
        public static CardData AddCreateScript(this CardData t, CardScript cs)
        {
            t.createScripts = new Il2CppReferenceArray<CardScript>(t.createScripts.AddItem(cs).ToArray());
            return t;
        }
        public static CardData AddCreateScripts(this CardData t, CardScript[] css)
        {
            foreach (var cs in css)
                t.AddCreateScript(cs);
            return t;
        }

        public static T CreateCardScript<T>(string modName = "API", string cardName = "CardScript", StatusEffectData effect = null, TraitData trait = null, Vector2Int range = default(Vector2Int), float multiply = 1f) where T : CardScript
        {
            var newData = ScriptableObject.CreateInstance<T>();
            newData.name = cardName.StartsWith(modName) ? cardName : $"{modName}.{cardName}";
            if (modName == "") newData.name = cardName;

            newData = newData.Set("effect", effect)
                .Set("trait", trait)
                .Set("multiply", multiply);

            newData.GetType().GetProperties().ToList()
                .Find(p => p.Name.ToLower().EndsWith("range"))
                ?.SetValue(newData, range);



            return newData;
        }

        public static T CreateTargetConstraint<T>(string cardName = "TargetConstraint", bool not = false, int value = 0, int moreThan = 0,
            StatusEffectData status = null, StatusEffectData effect = null, string statusType = "",
            TraitData trait = null, bool ignoreSilenced = false,
            CardType[] allowedTypes = null, CardData[] allowedCards = null, bool mustBeMiniboss = false)
            where T : TargetConstraint
        {
            var newData = ScriptableObject.CreateInstance<T>();
            newData.name = cardName;

            newData = newData
                .Set("not", not)
                .Set("value", value)
                .Set("moreThan", moreThan)
                .Set("status", status)
                .Set("effect", effect)
                .Set("statusType", statusType)
                .Set("trait", trait)
                .Set("ignoreSilenced", ignoreSilenced)
                .Set("allowedTypes", allowedTypes.ToRefArray())
                .Set("allowedCards", allowedCards.ToRefArray())
                .Set("mustBeMiniboss", mustBeMiniboss);
            //play on slot
            //targetconstraintor

            return newData;
        }


        public static CardType CardTypeLookup(string type)
        {
            return AddressableLoader.groups["CardType"].lookup[type].Cast<CardType>();
        }
        public static CardData CardDataLookup(string name)
        {
            if (AddressableLoader.groups["CardData"].lookup.ContainsKey(name)) return AddressableLoader.groups["CardData"].lookup[name].Cast<CardData>();
            else return AddressableLoader.groups["CardData"].lookup[Tables.Cards[name]].Cast<CardData>();
        }
        public static CardData ToCardData(this string name)
        {
            return AddressableLoader.groups["CardData"].lookup[name].Cast<CardData>();
        }

        public static CardUpgradeData ToCardUpgradeData(this string name)
        {
            return AddressableLoader.groups["CardUpgradeData"].lookup[name].Cast<CardUpgradeData>();
        }


        /// <summary>
        /// Creates a ScriptableAmount object to use with "Equal To" effects<br/>
        /// Specifically used with StatusEffectApplyX (inheritors) and StatusEffectBonusDamageEqualToX
        /// The name is auto-generated, e.g. FixedAmount 0, Gold Factor 0.02, CurrentScrap - 1
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="amount">For ScriptableFixedAmount</param>
        /// <param name="factor">For ScriptableGold (e.g. Greed: factor = 0.02)</param>
        /// <param name="statusType">
        /// For ScriptableCurrentStatus. Uses the .type property of the status effect this would apply to<para/>
        /// Vanilla types: block, demonize, freeaction (noomlin), frenzy, frost, haze, heal, ink, lumin, overload (overburn), scrap, snow, snowresist, shroom, shell, spice, teeth, vim (bom)<br/>
        /// Misc. types: [max] damage/counter/health up/down, nextphase, frostimmune, shroomresist, snowimmune, spiceresist, spiceimmune, vimimmune, stealth
        /// </param>
        /// <param name="offset">For ScriptableCurrentStatus. How much to add/reduce from current stacks</param>
        /// <returns></returns>
        public static T CreateScriptableAmount<T>(int amount = 0, float factor = 1f, string statusType = "snow", int offset = 0) where T : ScriptableAmount
        {
            var newData = ScriptableObject.CreateInstance<T>();
            var name = typeof(T).Name.Replace("Scriptable", "");
            switch (name)
            {
                case "FixedAmount":
                    newData.name = $"{name} {amount}";
                    newData = newData.Set("amount", amount);
                    break;
                case "Gold":
                    newData.name = $"{name} Factor {factor}";
                    newData = newData.Set("factor", factor);
                    break;
                case "CurrentStatus":
                    newData.name = $"Current{statusType.ToUpperFirstLetter()}";
                    newData.name += offset.ToString(" + #; - #;");
                    newData = newData.Set("statusType", statusType).Set("offset", offset);
                    break;
                default:
                    newData.name = name;
                    break;
            }
            return newData;
        }

        /// <summary>
        /// Only applies if applyEqualAmount is false
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="sa"></param>
        /// <returns></returns>
        public static T AddScriptableAmount<T>(this T t, ScriptableAmount sa) where T : StatusEffectData
        {
            return t.Set("scriptableAmount", sa);
        }

        // wip
        public static ScriptableAmount ToScriptableAmount(this string t)
        {
            if (t == "CurrentAttack")
            {
                return CreateScriptableAmount<ScriptableCurrentAttack>();
            }
            else if (t == "HealthLost")
            {
                return CreateScriptableAmount<ScriptableHealthLost>();
            }
            else
            {
                throw new ArgumentException("Unsupported type.");
            }
        }


        //special stuff for StatusEffectApplyXWhenUnitIsKilled

        /// <summary>
        /// Specifically used when the ScriptableAmount needed comes from another card (e.g. "Gain Their X")<br/>
        /// Only applies if applyEqualAmount is true<para/>
        /// 
        /// Examples:<br/>
        /// Devicro: When Ally Is Sacrificed Gain Their Attack<br/>
        /// When Enemy (Shroomed) Is Killed Apply Their Shroom To RandomEnemy
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="sa"></param>
        /// <returns></returns>
        public static T AddContextEqualAmount<T>(this T t, ScriptableAmount sa) where T : StatusEffectApplyX
        {
            t.contextEqualAmount = sa;
            t.applyEqualAmount = true;
            return t;
        }

        public static CardData.StatusEffectStacks StatusEffectStack<T>(this T se, int amount) where T : StatusEffectData
        {
            return new CardData.StatusEffectStacks
            {
                data = se,
                count = amount
            };
        }
        public static CardData.TraitStacks TraitStack<T>(this T se, int amount) where T : TraitData
        {
            return new CardData.TraitStacks
            {
                data = se,
                count = amount
            };
        }


        #endregion

        #region Special extensions

        /// <summary>
        /// Does an action when the BattleLog logs a hit.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="action"></param>
        /// <param name="attackerName"></param>
        /// <param name="damageRange"></param>
        /// <param name="damageType">
        /// Vanilla damageTypes: shroom, spikes, overload<br/>
        /// To add more: Edit the BattleLogSystem</param>
        /// <returns></returns>
        public static CardData AddWhenHitAction(this CardData c, Action<Hit> action, string? attackerName = null, (int min, int max)? damageRange = null, string? damageType = null, bool prefixPatch = false)
        {
            if (!prefixPatch)
                WFExtraBattlesPlugin.eventsOnHit.Add(new Func<Entity, Entity, int, string, Action<Hit>>((attacker, target, damage, _damageType) =>
                {
                    if (target.name == c.name
                    && attacker.name == (attackerName ?? attacker.name)
                    && (damageRange == null ? true : damage <= damageRange.Value.min && damage >= damageRange.Value.max)
                    && _damageType == (damageType ?? _damageType))
                        return action;
                    return (hit) => { };
                })
                );
            else
                WFExtraBattlesPlugin.eventsOnHitPre.Add(new Func<Entity, Entity, int, string, Action<Hit>>((attacker, target, damage, _damageType) =>
                {
                    if (target.name == c.name
                    && attacker.name == (attackerName ?? attacker.name)
                    && (damageRange == null ? true : damage <= damageRange.Value.min && damage >= damageRange.Value.max)
                    && _damageType == (damageType ?? _damageType))
                        return action;
                    return (hit) => { };
                })
                );
            return c;
        }
        public static CardData AddWhenHitPlayRingSFX(this CardData c, string? attackerName = null, (int min, int max)? damageRange = null, string? damageType = null)
        {
            return c.AddWhenHitAction((hit) => AddressableLoader.groups["GameModifierData"].list.ToArray().First().Cast<GameModifierData>().PlayRingSfx(), attackerName, damageRange, damageType);
        }

        //public static void PlayAudioClip(AudioClip audio) { LeanAudio.play(audio); }
        //public static CardData AddWhenHitPlaySoundfile(this CardData c, AudioClip audio, string? attackerName = null, (int min, int max)? damageRange = null, string? damageType = null) {return c.AddWhenHitAction((hit) => LeanAudio.play(audio));}

        public static CardData AddOnHitAction(this CardData c, Action<Hit> action, string? targetName = null, (int min, int max)? damageRange = null, string? damageType = null)
        {
            WFExtraBattlesPlugin.eventsOnHit.Add(new Func<Entity, Entity, int, string, Action<Hit>>((attacker, target, damage, _damageType) =>
            {
                if (target.name == (targetName ?? target.name)
                && attacker.name == c.name
                && (damageRange == null ? true : damage <= damageRange.Value.min && damage >= damageRange.Value.max)
                && _damageType == (damageType ?? _damageType))
                    return action;
                return (hit) => { };
            })
            );
            return c;
        }
        public static CardData AddOnHitPreAction(this CardData c, Action<Hit> action, string? targetName = null, (int min, int max)? damageRange = null, string? damageType = null)
        {
            WFExtraBattlesPlugin.eventsOnHitPre.Add(new Func<Entity, Entity, int, string, Action<Hit>>((attacker, target, damage, _damageType) =>
            {
                if (target.name == (targetName ?? target.name)
                && attacker.name == c.name
                && (damageRange == null ? true : damage <= damageRange.Value.min && damage >= damageRange.Value.max)
                && _damageType == (damageType ?? _damageType))
                    return action;
                return (hit) => { };
            })
            );
            return c;
        }

        // not working yet
        public static AudioClip LoadAudioFromCardPortraits(string name, AudioType audioType = AudioType.WAV)
        {
            var filepath = WildFrostAPIMod.ModsFolder + (name.EndsWith(".wav") ? name : (name + ".wav"));
            AudioClip audio = ES3.LoadAudio(WildFrostAPIMod.ModsFolder + (name.EndsWith(".wav") ? name : (name + ".wav")), audioType);
            return audio;
        }

        public static CardData AddDragAction(this CardData c, Action action)
        {
            WFExtraBattlesPlugin.eventsTryDrag.Add(new Func<Entity, Action>((entity) =>
            {
                if (entity.name == c.name)
                    return action;
                return () => { };
            })
            );
            return c;
        }

        public static CardData AddPreTurnAction(this CardData c, Action<Entity> action)
        {
            WFExtraBattlesPlugin.eventsProcessUnits.Add(new Func<Entity, Action<Entity>>((unit) =>
            {
                if (unit.name == c.name)
                    return action;
                return (unit) => { };
            })
            );
            return c;
        }

        public static CardData AddOnPlaceAction(this CardData c, Action<Entity> action)
        {
            WFExtraBattlesPlugin.eventsOnPlace.Add(new Func<Entity, Action<Entity>>((unit) =>
            {
                if (unit.name == c.name)
                    return action;
                return (unit) => { };
            })
            );
            return c;
        }


        #endregion

        public static T Set<T>(this T t, string property, object value)
        {
            var propertyInfo = t.GetType().GetProperty(property);
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                propertyInfo.SetValue(t, value);
            }
            return t;
        }

        public static T SetPrint<T>(this T t, string property, object value)
        {
            WFExtraBattlesPlugin.Log.LogDebug("start set");
            var propertyInfo = t.GetType().GetProperty(property);

            WFExtraBattlesPlugin.Log.LogDebug("prop get");
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                var org = propertyInfo.GetValue(t);
                WFExtraBattlesPlugin.Log.LogDebug("start write");
                propertyInfo.SetValue(t, value);

                WFExtraBattlesPlugin.Log.LogDebug($"finish write (did it change? {org == value})");
            }
            return t;


        }
        public static string Adjectivise(this string t)
        {
            var last = t[t.Length - 1];
            if (new char[] { 'g', 'm', 'n' }.Contains(last)) return t + last + 'y';
            if (new char[] { 'e', 'k', 'l', 'r', 't' }.Contains(last)) return t + "y";
            return t;
        }




        #region Debugging
        public static T SetCheckError<T>(this T t, string property, object value)
        {
            var propertyInfo = t.GetType().GetProperty(property);
            if (propertyInfo == null)
            {
                WFExtraBattlesPlugin.Log.LogError($"Object of type {t.GetType().Name} does not have a property named {property}");
            }

            if (!propertyInfo.CanWrite)
            {
                WFExtraBattlesPlugin.Log.LogError($"Property {property} of object {t.GetType().Name} is read-only");
            }

            if (!propertyInfo.PropertyType.IsAssignableFrom(value.GetType()))
            {
                WFExtraBattlesPlugin.Log.LogError($"Value of type {value.GetType().Name} cannot be assigned to property {property} of object {t.GetType().Name}");
            }

            propertyInfo.SetValue(t, value);
            return t;
        }
        public static T CompareWith<T>(this T t, T other, bool getDifferences = false)
        {
            foreach (var property in typeof(T).GetProperties())
            {
                var value1 = property.GetValue(t);
                var value2 = property.GetValue(other);
                if (getDifferences)
                    if (Equals(value1, value2)) break;
                try
                {
                    WFExtraBattlesPlugin.Log.LogDebug($"{property.Name}: {value1.ToString()} against {value2.ToString()}");
                }
                catch
                {
                    WFExtraBattlesPlugin.Log.LogError($"{property.Name} failed");
                }

            }
            return t;
        }



        #endregion

        #region Collection extensions

        public static Il2CppSystem.Collections.Generic.List<T> AddItem<T>(this Il2CppSystem.Collections.Generic.List<T> t, T item)
        {
            var l = new Il2CppSystem.Collections.Generic.List<T>();
            foreach (var v in t) l.Add(v); l.Add(item);
            return l;
        }
        public static T[] AddItem<T>(this T[] t, T item)
        {
            var l = new T[t.Length + 1];
            for (int i = 0; i <= t.Length; i++) l[i] = (i == t.Length) ? item : t[i];
            return l;
        }
        public static T[] ToArray<T>(this T t) where T : ScriptableObject
        {
            return new T[1] { t };
        }

        public static Il2CppReferenceArray<T> ToRefArray<T>(this T[] t) where T : ScriptableObject
        {
            return new Il2CppReferenceArray<T>(t ?? new T[0]);
        }
        public static Il2CppReferenceArray<T> ToRefArray<T>(this T t) where T : ScriptableObject
        {
            return new Il2CppReferenceArray<T>(new T[] { t } ?? new T[0]);
        }
        public static Il2CppSystem.Collections.Generic.List<T> ToIl2CppList<T>(this T t)
        {
            return new Il2CppSystem.Collections.Generic.List<T>().AddItem(t);
        }

        public static BattleWavePoolData.Wave[] ToArray(this BattleWavePoolData.Wave t)
        {
            return new BattleWavePoolData.Wave[1] { t };
        }
        public static Il2CppReferenceArray<BattleWavePoolData.Wave> ToRefArray(this BattleWavePoolData.Wave[] t)
        {
            return new Il2CppReferenceArray<BattleWavePoolData.Wave>(t ?? new BattleWavePoolData.Wave[0]);
        }
        #endregion

        #region CreateData (Cards)

        /// <summary>
        /// Using predefined arguments. Compound with .Set() if any aren't defined here
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="modName"></param>
        /// <param name="effectName"></param>
        /// <param name="newCard">The target card to summon. Has to already be in CardData group. (Renamed from .summonCard for clarity)</param>
        /// <param name="effectToApply">Requires applyToFlags. To constraint this, use .AddApplyConstraints()</param>
        /// <param name="applyToFlags">When used with applyConstraints, will only apply to those satisfying the flag AND the consraints</param>
        /// <param name="position">Used with StatusEffectSummon</param>
        /// <param name="onSacrifice"></param>
        /// <param name="onConsume"></param>
        /// <param name="bonusDamageSource">(Renamed from .on)</param>
        /// <param name="equalAmount"></param>
        /// <param name="factor"></param>
        /// <param name="type">used with StatusEffectApplyXWhenYAppliedTo, StatusEffectImmuneToX, TargetConstraintHasStatusType, StatusEffectBonusDamageEqualToX with ScriptableCurrentStatus etc</param>
        /// <returns></returns>
        public static T CreateStatusEffectData<T>(string modName, string effectName,
            string? newCard = null,
            StatusEffectData? effectToApply = null,
            StatusEffectInstantSummon.Position position = StatusEffectInstantSummon.Position.InFrontOf,
            StatusEffectApplyX.ApplyToFlags applyToFlags = StatusEffectApplyX.ApplyToFlags.None,
            bool onSacrifice = false,
            bool onConsume = false,
            StatusEffectBonusDamageEqualToX.On bonusDamageSource = StatusEffectBonusDamageEqualToX.On.Self,
            bool equalAmount = false,
            float factor = 1f, string type = "",
            bool stackable = false, bool canBeBoosted = false) where T : StatusEffectData
        {
            var se = StatusEffectAdder.CreateStatusEffectData<T>(modName, effectName);

            foreach (var property in typeof(T).GetProperties())
            {
                if (property != null && property.CanWrite && Equals(property.GetValue(se), true))
                {
                    //property.SetValue(se, false); // It's easier to avoid mistakes like this
                }
            }

            se = se
                .Set("type", type)
                .Set("targetConstraints", new Il2CppReferenceArray<TargetConstraint>(0))
                .Set("applyConstraints", new Il2CppReferenceArray<TargetConstraint>(0))
                .Set("effectToApply", effectToApply)
                .Set("targetSummon", effectToApply)
                .Set("position", position)
                .Set("applyToFlags", applyToFlags)
                .Set("sacrificed", onSacrifice)
                .Set("consumed", onConsume)
                .Set("on", bonusDamageSource)
                .Set("equalAmount", equalAmount)
                .Set("factor", factor)
                .Set("hiddenKeywords", new Il2CppReferenceArray<KeywordData>(0))
                .Set("doPing", false)
                .Set("targetMustBeAlive", false)
                .Set("stackable", stackable)
                .Set("canBeBoosted", canBeBoosted);


            if (typeof(T) == typeof(StatusEffectSummon))
            {
                se = se
                    .Set("setCardType", CardTypeLookup("Summoned"))
                    .Set("gainTrait", CardAdder.VanillaStatusEffects.TemporarySummoned.StatusEffectData())
                    .Set("effectPrefabRef", CardAdder.VanillaStatusEffects.SummonBeepop.StatusEffectData().Cast<StatusEffectSummon>().effectPrefabRef)
                    .Set("eventPriority", 99999);
                se = (newCard == null) ? se : se.Set("summonCard", newCard.ToCardData());
            }

            if (se != null)
            {
                if (!AddressableLoader.groups["StatusEffectData"].lookup.ContainsKey(se.name))
                {
                    AddressableLoader.groups["StatusEffectData"].list.Add(se);
                    AddressableLoader.groups["StatusEffectData"].lookup.Add(se.name, se);
                }
                //GeneralModifier.im.Print($"Effect {se.name} has been injected!");
                return se;
            }

            return default;
        }

        public static T CreateStatusEffectDataBase<T>(string modName, string effectName,
            bool isStatus = false,
            bool isReaction = false,
            bool isKeyword = false,
            string type = "",
            string keyword = "",
            string iconGroupName = "",
            bool visible = false,
            bool stackable = false,
            bool offensive = false,
            bool makesOffensive = false,
            bool doesDamage = false,
            bool canBeBoosted = false,
            int descOrder = 0,
            bool affectedBySnow = false,
            int eventPriority = 0,
            bool removeOnDiscard = false,
            TargetConstraint[] targetConstraints = default
            ) where T : StatusEffectData
        {
            var se = StatusEffectAdder.CreateStatusEffectData<T>(modName, effectName);
            {
                se.isStatus = isStatus;
                se.isReaction = isReaction;
                se.isKeyword = isKeyword;
                se.type = type;
                se.iconGroupName = iconGroupName;
                se.visible = visible;
                se.stackable = stackable;
                se.offensive = offensive;
                se.makesOffensive = makesOffensive;
                se.doesDamage = doesDamage;
                se.canBeBoosted = canBeBoosted;
                se.descOrder = descOrder;
                se.affectedBySnow = affectedBySnow;
                se.eventPriority = eventPriority;
                se.removeOnDiscard = removeOnDiscard;
                se.targetConstraints = targetConstraints.ToRefArray();
            }

            return se;
        }



        // Using Dictionary to emulate the kwargs argument type (not working fully idk)
        public static T CreateStatusEffectDataDict<T>(string modName, string effectName,
            Dictionary<string, object> kwargs) where T : StatusEffectData
        {
            var se = StatusEffectAdder.CreateStatusEffectData<T>(modName, effectName)
                .Set("type", "")
                .Set("targetConstraints", new Il2CppReferenceArray<TargetConstraint>(0));

            foreach (var prop in kwargs)
                se = se.Set(prop.Key, prop.Value);

            if (typeof(T) == typeof(StatusEffectSummon))
                se = se
                    .Set("setCardType", CardTypeLookup("Summoned"))
                    .Set("gainTrait", CardAdder.VanillaStatusEffects.TemporarySummoned.StatusEffectData())
                    .Set("effectPrefabRef", CardAdder.VanillaStatusEffects.SummonBeepop.StatusEffectData().Cast<StatusEffectSummon>().effectPrefabRef);

            if (se != null)
            {
                if (!AddressableLoader.groups["StatusEffectData"].lookup.ContainsKey(se.name))
                {
                    AddressableLoader.groups["StatusEffectData"].list.Add(se);
                    AddressableLoader.groups["StatusEffectData"].lookup.Add(se.name, se);
                }
                //GeneralModifier.im.Print($"Effect {se.name} has been injected!");
                return se;
            }

            return default;
        }



        public static KeywordData CreateKeywordData(string modName, string keywordName,
            string title = "",
            string desc = "",
            bool show = true,
            string iconTintHex = "FFCA57")
        {
            var barrage = AddressableLoader.groups["KeywordData"].lookup["barrage"].Cast<KeywordData>();

            var kd = ScriptableObject.CreateInstance<KeywordData>();

            kd.name = ((modName == "") ? keywordName : modName + keywordName).ToLower();
            kd.panelSprite = barrage.panelSprite;
            kd.panelColor = barrage.panelColor;
            kd.iconTintHex = iconTintHex;
            kd.titleKey = FromId(CreateLocalizedString(kd.name + ".Title", title));
            kd.descKey = FromId(CreateLocalizedString(kd.name + ".Desc", desc));
            kd.show = true; // show description
            kd.showName = false;
            kd.showIcon = false;
            kd.canStack = false;
            //kd.iconName = "";


            return kd;
        }


        public static TraitData CreateTraitData(string modName, string traitName,
            KeywordData keyword,
            StatusEffectData[] effects = default,
            TraitData[] overrideTraits = default,
            bool isReaction = false)
        {
            var td = ScriptableObject.CreateInstance<TraitData>();
            td.name = $"{modName}.{traitName}";
            td.keyword = keyword;
            td.effects = effects?.ToRefArray();
            td.overrides = overrideTraits?.ToRefArray();
            td.isReaction = isReaction;

            if (td != null)
            {
                if (!AddressableLoader.groups["TraitData"].lookup.ContainsKey(td.name))
                {
                    AddressableLoader.groups["TraitData"].list.Add(td);
                    AddressableLoader.groups["TraitData"].lookup.Add(td.name, td);
                }
                //GeneralModifier.im.Print($"Trait {td.name} has been injected!");
                return td;
            }

            return default;
        }

        /// <summary>
        /// Register data in its AddressableLoader.groups
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="quiet"></param>
        /// <returns></returns>
        public static T RegisterInGroup<T>(this T t, bool quiet = false) where T : ScriptableObject
        {
            var ping = false;

            if (t is StatusEffectData)
            {
                if (!AddressableLoader.groups["StatusEffectData"].lookup.ContainsKey(t.name))
                {
                    AddressableLoader.groups["StatusEffectData"].list.Add(t);
                    AddressableLoader.groups["StatusEffectData"].lookup.Add(t.name, t);
                    ping = true;
                }
            }
            else if (t is KeywordData)
            {
                if (!AddressableLoader.groups["KeywordData"].lookup.ContainsKey(t.name.ToLower()))
                {
                    AddressableLoader.groups["KeywordData"].list.Add(t);
                    AddressableLoader.groups["KeywordData"].lookup.Add(t.name.ToLower(), t);
                    ping = true;
                }
            } // keywords have to be lowercase
            else if (!AddressableLoader.groups[typeof(T).Name].lookup.ContainsKey(t.name))
            {
                AddressableLoader.groups[typeof(T).Name].list.Add(t);
                AddressableLoader.groups[typeof(T).Name].lookup.Add(t.name, t);
                ping = true;
            }
            if (ping && !quiet) WFExtraBattlesPlugin.Log.LogDebug($"{typeof(T).Name.Replace("Data", "")} {t.name} injected by api!");
            return t;
        }

        public static void RegisterInGroup(ScriptableObject[] list, bool quiet = false)
        {
            foreach (var t in list) t.RegisterInGroup(quiet);
        }

        #endregion

        #region CreateData (Nodes)

        // testing out string version
        public static BattleWavePoolData CreateBattleWavePoolData(string modName, string titleName,
            string wavePoolName = "Wave Pool 1",
            BattleWavePoolData.Wave[] waves = default,
            List<string> unitList = null,
            int pullCount = 1,
            int weight = 1
            )
        {
            if (waves == null && unitList == null)
                throw new ArgumentNullException("Either waves or wavesList must be non-null");

            var l = new Il2CppSystem.Collections.Generic.List<CardData>();
            if (unitList != null)
            {
                foreach (var unit in unitList) l.Add(unit.ToCardData());
                var wave1 = new BattleWavePoolData.Wave
                {
                    units = l,
                    fixedOrder = false,
                    maxSize = 6,
                    value = 0, // idk what this does tbh
                    positionPriority = 0 // this either
                };
                waves = waves.AddItem(wave1);
            }

            var se = ScriptableObject.CreateInstance<BattleWavePoolData>()
                .Set("name", ((modName == "") ? titleName : $"{modName}.{titleName}") + $" {wavePoolName}")
                .Set("waves", waves.ToRefArray())
                .Set("forcePulls", pullCount).Set("maxPulls", pullCount)
                .Set("weight", weight);

            return se;
        }

        internal static bool WaveContainsEnemyLeader(BattleWavePoolData.Wave wave)
        {
            foreach (var unit in wave.units)
                if (new List<string>() { "Miniboss", "Boss", "BossSmall" }.Contains(unit.cardType.name))
                    return true;
            return false;
        }

        internal static bool PoolAlwaysContainsEnemyLeader(BattleWavePoolData pool)
        {
            foreach (var wave in pool.waves)
                if (!WaveContainsEnemyLeader(wave))
                    return false;
            return true;
        }


        /// <summary>
        /// MUST CONTAIN A MINIBOSS/BOSS<br/>Create a battle that you can test using Next Battle
        /// </summary>
        /// <param name="modName"></param>
        /// <param name="titleName"></param>
        /// <param name="nodeName">The name to show on the map</param>
        /// <param name="pools">Generated from CreateBattleWavePoolData()</param>
        /// <param name="sprite">The sprite to show on the map</param>
        /// <param name="goldGivers">Vanilla settings: 0 for bosses, 1 otherwise</param>
        /// <param name="bonusUnitPool">These show up in wave 2 (the bell is NOT necessary)</param>
        /// <param name="bonusUnitRange">
        /// The possible range of bonus units to add<br/>
        /// This chooses a random enemy from the pool for each 1.
        /// </param>
        /// <returns></returns>
        public static BattleData CreateBattleData(string modName, string titleName,
            string nodeName,
            BattleWavePoolData[] pools,
            List<string> poolsList = null,
            Sprite sprite = null,
            int goldGivers = 1,
            CardData[] bonusUnitPool = default(CardData[]),
            Vector2Int bonusUnitRange = default(Vector2Int)
            )
        {
            if (pools == null && poolsList == null)
                throw new ArgumentNullException("Either pools or poolsList must be non-null");

            #region fallback enemy leader
            bool enemyLeader = false;
            foreach (var pool in pools)
                if (PoolAlwaysContainsEnemyLeader(pool))
                    enemyLeader = true;

            if (!enemyLeader)
            {
                var gnome = CardDataLookup("NakedGnome");
                var bossGnome = CardAdder.CreateCardData("", "ApologyGnome")
                        .SetTitle("Apology Gnome")
                        .SetIsUnit()
                        .SetCardType(CardAdder.VanillaCardTypes.Miniboss)
                        .SetCanPlay(CardAdder.CanPlay.CanPlayOnEnemy | CardAdder.CanPlay.CanPlayOnBoard)
                        .SetSprites(gnome.mainSprite, gnome.backgroundSprite)
                        .SetStats(1, null, 0)
                        .SetBloodProfile(gnome.bloodProfile)
                        .SetIdleAnimationProfile(gnome.idleAnimationProfile)
                        .SetFlavour("Oops... the devs forgot to put a boss here!")
                        .RegisterInGroup();

                pools = pools.AddItem(CreateBattleWavePoolData(modName, titleName, wavePoolName: "Boss Wave Pool",
                waves: (new BattleWavePoolData.Wave { units = bossGnome.ToIl2CppList(), value = 0, maxSize = 1, positionPriority = 1 }).ToArray(),
                pullCount: 1));
            }
            WFExtraBattlesPlugin.Log.LogDebug("Sending out the gnome? " + !enemyLeader);
            #endregion

            var template = AddressableLoader.groups["BattleData"].lookup["Snowbos"].Cast<BattleData>();
            var se = ScriptableObject.CreateInstance<BattleData>()
                .Set("name", (modName == "") ? titleName : $"{modName}.{titleName}")
                .SetText(nodeName, xKey: "nameRef")
                .Set("sprite", sprite ?? CardAdder.LoadSpriteFromCardPortraits("CardPortraits\\FALLBACKBATTLESPRITE"))
                .Set("pointFactor", 1f)
                .Set("waveCounter", 5)
                .Set("pools", pools.Reverse().ToArray().ToRefArray())
                .Set("goldGivers", goldGivers)
                .Set("goldGiverPool", template.goldGiverPool)
                .Set("bonusUnitPool", bonusUnitPool.ToRefArray())
                .Set("bonusUnitRange", bonusUnitRange)
                .Set("generationScript", template.generationScript) // remember to change this to allow custom Frost Guardian fights etc
                .Set("setUpScript", template.setUpScript);

            return se;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type">Units, Charms, Items</param>
        /// <param name="list"></param>
        /// <param name="copies"></param>
        /// <returns></returns>
        public static RewardPool CreateRewardPool(string type = "Units", DataFile[] list = default, int copies = 1)
        {
            var data = ScriptableObject.CreateInstance<RewardPool>();
            data.type = type;
            data.copies = copies;
            data.list = list.ToRefArray().ToList();

            return data;
        }



        /// <summary>
        /// please remember to assign the leader cardtype to your leaders, or units won't process
        /// i'm too lazy to figure out how to clone and set them
        /// </summary>
        /// <param name="name"></param>
        /// <param name="startingInventory"></param>
        /// <param name="leaders"></param>
        /// <param name="rewardPools"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public static ClassData CreateClassData(string name, Inventory startingInventory = null, CardData[] leaders = null, RewardPool[] rewardPools = null, Sprite flag = null)
        {
            var basic = AddressableLoader.groups["GameMode"].lookup["GameModeNormal"].Cast<GameMode>().classes[0];

            var newLeaders = leaders?.Select(
                card => card.Clone().Set("cardType", basic.leaders[0].cardType)
                .AddCreateScript(basic.leaders[0].createScripts[1])).ToArray();

            ClassData data = ScriptableObject.CreateInstance<ClassData>();
            data.name = name;
            data.requiresUnlock = basic.requiresUnlock;
            data.characterPrefab = basic.characterPrefab;
            data.startingInventory = startingInventory ?? basic.startingInventory;
            data.leaders = newLeaders?.ToRefArray() ?? basic.leaders;
            data.rewardPools = rewardPools?.ToRefArray() ?? basic.rewardPools;
            data.flag = flag ?? CardAdder.LoadSpriteFromCardPortraits("CardPortraits\\FALLBACKBATTLESPRITE");

            return data;
        }

        public static ClassData AddClass(this ClassData data)
        {
            WFExtraBattlesPlugin.ClassDataAdditions.Add(data);
            return data;
        }

        #endregion

        #region Miya stuff

        /// <summary>
        /// Set descriptions for StatusEffects or KeywordData. If neither of these types AND it doesn't use textKey, then write it in xKey
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="text">Acts as "title" when used with KeywordData</param>
        /// <param name="desc">Used with KeywordData</param>
        /// <param name="tableRef">
        /// Where the text is stored. Completely optional<br/>
        /// I put it in Credits because it's emptier
        /// </param>
        /// <param name="xKey">Used only if it's not StatusEffect/Keyword and doesn't accept textKey</param>
        /// <returns></returns>
        public static T SetText<T>(this T t, string text, string tableRef = "Credits", string xKey = "textKey") where T : ScriptableObject
        {
            if ((new string[] { "textKey", "descKey" }).Contains(xKey))
                return t.Set("textKey", FromId(CreateLocalizedString(t.name + ".Text", text, tableRef), tableRef))
                    .Set("descKey", FromId(CreateLocalizedString(t.name + ".Desc", text, tableRef), tableRef));
            else return t.Set(xKey, FromId(CreateLocalizedString(t.name + $".{xKey.Replace("Key", "").ToUpperFirstLetter()}", text, tableRef)));
        }
        public static T SetTitle<T>(this T t, string text, string tableRef = "Credits", string xKey = "textKey") where T : ScriptableObject
        {
            return t.Set("titleKey", FromId(CreateLocalizedString(t.name + ".Title", text, tableRef), tableRef));
        }

        public static LocalizedString FromId(long id, string tableRef = "Credits")
        {
            return new LocalizedString(TableReference.TableReferenceFromString(tableRef), new TableEntryReference
            {
                KeyId = id,
                ReferenceType = TableEntryReference.Type.Id
            });
        }

        public static long CreateLocalizedString(string key, string localized, string tableRef = "Credits")
        {
            StringTable table = LocalizationSettings.StringDatabase.GetTable(TableReference.TableReferenceFromString(tableRef));
            StringTableEntry stringTableEntry = table.AddEntry(key, localized);
            return stringTableEntry.KeyId;
        }

        #endregion

        #region Jacorb stuff (mostly dealing with il2cppDictionary shenanigans)

        public static Dictionary<string, UnityEngine.Object> ToStandard(this Il2CppSystem.Collections.Generic.Dictionary<string, UnityEngine.Object> dict)
        {
            var entries = dict._entries.ToArray();
            var convEntries = entries.Where(e => e.key is not null).Select(e => new KeyValuePair<string, UnityEngine.Object>(e.key, e.value)).ToArray();
            return new Dictionary<string, UnityEngine.Object>(convEntries);
        }

        public static Dictionary<string, N> WideCast<N>(this Dictionary<string, UnityEngine.Object> dict) where N : Il2CppObjectBase
        {
            return new Dictionary<string, N>(dict.Select(kvp => new KeyValuePair<string, N>(kvp.Key, kvp.Value.Cast<N>())));
        }

        public static Sprite LoadSprite(string baseLoc, string loc, Vector2? pivot = null)
        {
            Texture2D texture2D = new(2, 2);
            texture2D.LoadImage(File.ReadAllBytes(baseLoc + (loc.EndsWith(".png") ? loc : (loc + ".png"))));
            return texture2D.ToSprite(pivot);
        }

        #endregion
    }
}
