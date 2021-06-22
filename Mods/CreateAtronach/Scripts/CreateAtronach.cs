// Project:   Create Atronach Mod for Daggerfall Unity
// Author:    DunnyOfPenwick
// Origin Date:  June 2021

using System;
using System.Collections;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallConnect.FallExe;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Utility;

namespace CreateAtronachMod
{
    public class CreateAtronach : BaseEntityEffect
    {
        private string effectKey = "CreateAtronach";

        // Variant can be stored internally with any format
        struct VariantProperties
        {
            public string subGroupKey;
            public EffectProperties effectProperties;
            public MobileTypes mobileType;
            public Color32 eggColor;
            public ItemGroups itemGroup;
            public int itemIndex;
        }

        private readonly VariantProperties[] variants = new VariantProperties[]
        {
            new VariantProperties()
            {
                subGroupKey = "Fire",
                mobileType = MobileTypes.FireAtronach,
                eggColor = new Color32(255, 140, 4, 255),
                itemGroup = ItemGroups.MetalIngredients,
                itemIndex = (int) MetalIngredients.Sulphur
            },
            new VariantProperties()
            {
                subGroupKey = "Flesh",
                mobileType = MobileTypes.FleshAtronach,
                eggColor = new Color32(20, 175, 20, 255),
                itemGroup = ItemGroups.MiscellaneousIngredients1,
                itemIndex = (int) MiscellaneousIngredients1.Elixir_vitae
            },
            new VariantProperties()
            {
                subGroupKey = "Ice",
                mobileType = MobileTypes.IceAtronach,
                eggColor = new Color32(50, 100, 255, 255),
                itemGroup = ItemGroups.Gems,
                itemIndex = (int) Gems.Turquoise
            },
            new VariantProperties()
            {
                subGroupKey = "Iron",
                mobileType = MobileTypes.IronAtronach,
                eggColor = new Color32(80, 80, 90, 255),
                itemGroup = ItemGroups.MetalIngredients,
                itemIndex = (int) MetalIngredients.Lodestone
            }
        };

        private const string groupName = "Create Atronach";
        private const string displayNameText = "Create {0} Atronach";
        private const string missingComponentText = "You need {0} to create a {1} Atronach.";


        // Must override Properties to return correct properties for any variant
        // The currentVariant value is set by magic framework - each variant gets enumerated to its own effect template
        public override EffectProperties Properties
        {
            get { return variants[currentVariant].effectProperties; }
        }


        public override void SetProperties()
        {
            properties.Key = effectKey;
            properties.ShowSpellIcon = false;
            properties.AllowedTargets = TargetTypes.CasterOnly;
            properties.AllowedElements = ElementTypes.Magic;
            properties.AllowedCraftingStations = MagicCraftingStations.SpellMaker;
            properties.MagicSkill = DFCareer.MagicSkills.Mysticism;
            properties.DisableReflectiveEnumeration = true;
            properties.SupportChance = true;
            properties.ChanceFunction = ChanceFunction.Custom;
            properties.ChanceCosts = MakeEffectCosts(24, 110, 140);
            properties.SupportMagnitude = true;
            properties.MagnitudeCosts = MakeEffectCosts(28, 130);

            // Set variant count so framework knows how many to extract
            variantCount = variants.Length;

            // Set properties unique to each variant
            for (int i = 0; i < variantCount; ++i)
            {
                variants[i].effectProperties = properties; //making a copy of default properties struct
                variants[i].effectProperties.Key = string.Format("{0}-{1}", effectKey, variants[i].subGroupKey);
            }
        }

        public override string GroupName => groupName;
        public override string SubGroupName => variants[currentVariant].subGroupKey;
        public override string DisplayName => string.Format(displayNameText, SubGroupName);
        public override TextFile.Token[] SpellMakerDescription => GetSpellMakerDescription();
        public override TextFile.Token[] SpellBookDescription => GetSpellBookDescription();


        public override void Start(EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Start(manager, caster);

            if (caster == null)
            {
                return;
            }

            //requires spell component, abort spell effect if component is not in caster inventory
            DaggerfallUnityItem ingredient = GetSpellComponent();
            if (ingredient == null)
            {
                AbortSpellEffect(manager);

                return;
            }

            Vector3 location;
            if (TryGetSpawnLocation(out location))
            {
                Caster.Entity.Items.RemoveOne(ingredient);
                Summon(location);
            }
            else
            {
                //couldn't find a good spot to park the atronach
                AbortSpellEffect(manager);
            }
        }


        public override bool RollChance()
        {
            int modifiedChance = ChanceValue();
            modifiedChance += Caster.Entity.Stats.GetLiveStatValue(DFCareer.Stats.Willpower) / 3;
            bool outcome = Dice100.SuccessRoll(modifiedChance);

            return outcome;
        }


        private DaggerfallUnityItem GetSpellComponent()
        {
            VariantProperties variant = variants[currentVariant];
            ItemTemplate itemTemplate = DaggerfallUnity.Instance.ItemHelper.GetItemTemplate(variant.itemIndex);

            DaggerfallUnityItem item = Caster.Entity.Items.GetItem(variant.itemGroup, variant.itemIndex, false, false, false);

            if (item == null)
            {
                string msg = string.Format(missingComponentText, itemTemplate.name, variant.subGroupKey);
                DaggerfallUI.AddHUDText(msg, 3.0f);
            }

            return item;
        }



        //Abort this spell effect and refund magicka cost to caster
        private void AbortSpellEffect(EntityEffectManager manager)
        {
            if (manager.ReadySpell != null)
            {
                foreach (EffectEntry entry in manager.ReadySpell.Settings.Effects)
                {
                    if (entry.Key == Key && entry.Settings.Equals(Settings))
                    {
                        FormulaHelper.SpellCost cost = FormulaHelper.CalculateEffectCosts(this, Settings, Caster.Entity);
                        Caster.Entity.IncreaseMagicka(cost.spellPointCost);
                        break;
                    }
                }
            }

            End();
        }

        private static readonly float[] scanDistances = { 2.0f, 3.0f, 1.2f };
        private static readonly float[] scanDownUpRots = { 45, 30, 0, -30, -45 };
        private static readonly float[] scanLefRightRots = { 0, 5, -5, 15, -15, 30, -30, 45, -45 };

        private bool TryGetSpawnLocation(out Vector3 location)
        {
            location = Vector3.zero;

            int casterLayerMask = ~(1 << Caster.gameObject.layer);

            //try to find reasonable spawn location in front of the caster
            foreach (float distance in scanDistances)
            {
                foreach (float downUpRot in scanDownUpRots)
                {
                    foreach (float leftRightRot in scanLefRightRots)
                    {
                        Quaternion rotation = Quaternion.Euler(downUpRot, leftRightRot, 0);
                        Vector3 direction = (Caster.transform.rotation * rotation) * Vector3.forward;

                        //shouldn't be anything between the caster and spawn point
                        Ray ray = new Ray(Caster.transform.position, direction);
                        RaycastHit hit; //might be useful for debugging
                        if (Physics.Raycast(ray, out hit, distance, casterLayerMask))
                        {
                            continue;
                        }

                        //create a reasonably sized capsule to check if enough space is available for spawning
                        Vector3 scannerPos = Caster.transform.position + (direction * distance);
                        Vector3 top = scannerPos + Vector3.up * 0.4f;
                        Vector3 bottom = scannerPos - Vector3.up * 0.4f;
                        float radius = 0.4f; //radius*2 included in height
                        if (!Physics.CheckCapsule(top, bottom, radius))
                        {
                            //just returning first available valid position
                            location = scannerPos;
                            return true;
                        }
                    }
                }
            }

            return false;
        }



        private void Summon(Vector3 location)
        {
            VariantProperties variant = variants[currentVariant];

            string displayName = string.Format("Summoned [{0}]", variant.mobileType.ToString());

            Transform parent = GameObjectHelper.GetBestParent();

            GameObject go = GameObjectHelper.InstantiatePrefab(DaggerfallUnity.Instance.Option_EnemyPrefab.gameObject, displayName, parent, location);

            go.SetActive(false);

            SetupDemoEnemy setupEnemy = go.GetComponent<SetupDemoEnemy>();

            //gender isn't really needed for atronachs, but we'll tack it on regardless
            MobileGender gender = (UnityEngine.Random.Range(0f, 1f) < 0.5f) ? MobileGender.Male : MobileGender.Female;

            // Configure summons
            bool allied = ChanceSuccess;
            setupEnemy.ApplyEnemySettings(variant.mobileType, MobileReactions.Hostile, gender, 0, allied);
            setupEnemy.AlignToGround();

            //additional magnitude-related adjustments
            AdjustAtronach(go);

            DaggerfallEnemy creature = go.GetComponent<DaggerfallEnemy>();

            //needs a loadID to save/serialize
            creature.LoadID = DaggerfallUnity.NextUID;

            GameManager.Instance.RaiseOnEnemySpawnEvent(go);

            //have atronach looking in same direction as caster
            creature.transform.rotation = Caster.transform.rotation;

            Texture2D eggTexture = CreateAtronachMod.Instance.SummoningEggTexture;
            AudioClip sound = CreateAtronachMod.Instance.WarpIn;
            SummoningEgg egg = new SummoningEgg(creature, eggTexture, variant.eggColor, sound);

            //start coroutine to animate the 'hatching' process
            IEnumerator coroutine = egg.Hatch();
            CreateAtronachMod.Instance.StartCoroutine(coroutine);
        }


        private void AdjustAtronach(GameObject atronach)
        {
            int magnitude = Mathf.Clamp(GetMagnitude(caster), 1, 100);

            MobileUnit mobileUnit = atronach.GetComponentInChildren<MobileUnit>();

            //other atronachs in the game have random health with a large range
            //we want ours tied to spell magnitude
            int luckBonus = Caster.Entity.Stats.GetLiveStatValue(DFCareer.Stats.Luck) / 10;
            MobileEnemy mobileEnemy = mobileUnit.Enemy;
            mobileEnemy.MinHealth += magnitude - 1;
            mobileEnemy.MaxHealth = mobileEnemy.MinHealth + luckBonus;

            //Record MobileEnemy changes to the MobileUnit
            mobileUnit.SetEnemy(DaggerfallUnity.Instance, mobileEnemy, MobileReactions.Hostile, 0);

            DaggerfallEntityBehaviour behaviour = atronach.GetComponent<DaggerfallEntityBehaviour>();
            EnemyEntity entity = behaviour.Entity as EnemyEntity;

            //Since we made changes to MobileEnemy, we have to reset the enemy career
            entity.SetEnemyCareer(mobileEnemy, behaviour.EntityType);
        }


        private TextFile.Token[] GetSpellMakerDescription()
        {
            return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                TextFile.Formatting.JustifyCenter,
                DisplayName,
                GetEffectDescription(),
                "Duration: Permanent",
                "Chance: Determines if atronach is allied with its creator",
                "Magnitude: Determines toughness");
        }

        private TextFile.Token[] GetSpellBookDescription()
        {
            return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                TextFile.Formatting.JustifyCenter,
                DisplayName,
                "Duration: Permanent",
                "Chance: Determines if allied to creator, %bch + %ach per %clc level(s);",
                "modified by Willpower",
                "Magnitude: Determines toughness, %1bm - %2bm + %1am - %2am per %clm level(s)",
                "",
                "\"" + GetEffectDescription() + "\"",
                "[Mysticism]");
        }


        private const string effectDescriptionA = "Creates a {0} Atronach; requires {1}.";
        private const string effectDescriptionAn = "Creates an {0} Atronach; requires {1}.";

        private string GetEffectDescription()
        {
            string description = currentVariant <= 1 ? effectDescriptionA : effectDescriptionAn;
            ItemTemplate item = DaggerfallUnity.Instance.ItemHelper.GetItemTemplate(variants[currentVariant].itemIndex);
            return string.Format(description, SubGroupName, item.name);
        }

    }
}
