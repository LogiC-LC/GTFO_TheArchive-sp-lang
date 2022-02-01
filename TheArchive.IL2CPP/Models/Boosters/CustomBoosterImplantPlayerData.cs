﻿using DropServer;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using TheArchive.Interfaces;
using TheArchive.Managers;
using TheArchive.Utilities;

namespace TheArchive.Models.Boosters
{
    public class CustomBoosterImplantPlayerData
    {
        public static int CurrencyNewBoosterCost { get; set; } = 1000;
        public static float CurrencyGainMultiplier { get; set; } = 1f;

        /// <summary>
        /// Muted Boosters
        /// </summary>
        public CustomCategory Basic { get; set; } = new CustomCategory(BoosterImplantCategory.Muted);
        /// <summary>
        /// Bold Boosters
        /// </summary>
        public CustomCategory Advanced { get; set; } = new CustomCategory(BoosterImplantCategory.Bold);
        /// <summary>
        /// Agressive Boosters
        /// </summary>
        public CustomCategory Specialized { get; set; } = new CustomCategory(BoosterImplantCategory.Aggressive);
        /// <summary>
        /// Array of Boosters InstanceIds that are new (in game popup in lobby screen)
        /// </summary>
        public uint[] New { get; set; } = new uint[0];

        public CustomBoosterImplantPlayerData() { }

        /// <summary>
        /// Acknowledge the amount of boosters missed
        /// </summary>
        /// <param name="acknowledgeMissed"></param>
        public void AcknowledgeMissedBoostersWithIds(CustomBoosterTransaction.CustomMissed acknowledgeMissed)
        {
            Basic.MissedAck = acknowledgeMissed.Basic;
            Advanced.MissedAck = acknowledgeMissed.Advanced;
            Specialized.MissedAck = acknowledgeMissed.Specialized;
        }

        /// <summary>
        /// Get all categories where a new booster should be generated for.
        /// </summary>
        /// <returns></returns>
        public CustomCategory[] GetCategoriesWhereCurrencyCostHasBeenReached()
        {
            var cats = new List<CustomCategory>();

            if (Basic.Currency > CurrencyNewBoosterCost) cats.Add(Basic);
            if (Advanced.Currency > CurrencyNewBoosterCost) cats.Add(Advanced);
            if (Specialized.Currency > CurrencyNewBoosterCost) cats.Add(Specialized);

            return cats.ToArray();
        }

        /// <summary>
        /// Acknowledge newly aquired boosters. (Done by closing the popup in game)
        /// </summary>
        /// <param name="boostersToAcknowledge"></param>
        public void AcknowledgeBoostersWithIds(uint[] boostersToAcknowledge)
        {
            var newNew = new List<uint>();
            foreach(var id in New)
            {
                if(boostersToAcknowledge.Any(idToAcknowledge => id == idToAcknowledge))
                {
                    continue;
                }
                newNew.Add(id);
            }
            New = newNew.ToArray();
        }

        /// <summary>
        /// Remove the (!) and "New" indicators in game by "touching" or interacting with the booster.
        /// </summary>
        /// <param name="boostersThatWereTouched"></param>
        public void SetBoostersTouchedWithIds(uint[] boostersThatWereTouched)
        {
            Basic.SetBoostersTouchedWithIds(boostersThatWereTouched);
            Advanced.SetBoostersTouchedWithIds(boostersThatWereTouched);
            Specialized.SetBoostersTouchedWithIds(boostersThatWereTouched);
        }

        /// <summary>
        /// Use up 1 charge nad remove if they're used up
        /// </summary>
        /// <param name="boostersToBeConsumed"></param>
        public void ConsumeBoostersWithIds(uint[] boostersToBeConsumed)
        {
            Basic.ConsumeOrDropBoostersWithIds(boostersToBeConsumed);
            Advanced.ConsumeOrDropBoostersWithIds(boostersToBeConsumed);
            Specialized.ConsumeOrDropBoostersWithIds(boostersToBeConsumed);
        }

        /// <summary>
        /// Drop as in remove those boosters from the inventory
        /// </summary>
        /// <param name="boostersToBeDropped"></param>
        public void DropBoostersWithIds(uint[] boostersToBeDropped)
        {
            Basic.DropBoostersWithIds(boostersToBeDropped);
            Advanced.DropBoostersWithIds(boostersToBeDropped);
            Specialized.DropBoostersWithIds(boostersToBeDropped);
        }

        public void AddCurrency(EndSessionRequest.PerBoosterCategoryInt boosterCurrency)
        {
            Basic.Currency += (int) (boosterCurrency.Basic * CurrencyGainMultiplier);
            Advanced.Currency += (int) (boosterCurrency.Advanced * CurrencyGainMultiplier);
            Specialized.Currency += (int) (boosterCurrency.Specialized * CurrencyGainMultiplier);
        }

        /// <summary>
        /// Add Booster into the Category and set it as new.
        /// </summary>
        /// <param name="newBooster"></param>
        /// <returns>true if the Booster has been added</returns>
        public bool TryAddBooster(CustomDropServerBoosterImplantInventoryItem newBooster)
        {
            if(!GetCategory(newBooster.Category).TryAddBooster(newBooster))
                return false;

            var newNew = new uint[New.Length + 1];

            for(int i = 0; i < New.Length; i++)
            {
                newNew[i] = New[i];
            }

            newNew[New.Length] = newBooster.InstanceId;

            New = newNew;

            return true;
        }

        public uint[] GetUsedIds()
        {
            var cats = GetAllCategories();

            var usedIds = new List<uint>();
            foreach (var cat in cats)
            {
                foreach(var id in cat.GetUsedIds())
                {
                    usedIds.Add(id);
                }
            }

            return usedIds.ToArray();
        }

        public CustomCategory[] GetAllCategories()
        {
            return new CustomCategory[]
            {
                Basic,
                Advanced,
                Specialized
            };
        }

        public CustomCategory GetCategory(BoosterImplantCategory category)
        {
            switch(category)
            {
                case BoosterImplantCategory.Muted:
                    return Basic;
                case BoosterImplantCategory.Bold:
                    return Advanced;
                case BoosterImplantCategory.Aggressive:
                    return Specialized;
            }
            return null;
        }

        public object ToBaseGame() => ToBaseGame(this);

        public static object ToBaseGame(CustomBoosterImplantPlayerData customData)
        {
            return ImplementationInstanceManager.ToBaseGameConverter(customData);
        }

        public static CustomBoosterImplantPlayerData FromBaseGame(object BoosterImplantPlayerData)
        {
            return ImplementationInstanceManager.FromBaseGameConverter<CustomBoosterImplantPlayerData>(BoosterImplantPlayerData);
        }

        public class CustomCategory
        {
            [JsonIgnore]
            public const int MAX_BOOSTERS_R5 = 10;
            [JsonIgnore]
            public const int MAX_BOOSTERS_R6 = 20;

            // Helper
            public BoosterImplantCategory CategoryType { get; set; } = BoosterImplantCategory.Muted;

            /// <summary> 1000 -> 100% -> new booster </summary>
            public int Currency { get; set; } = 0;
            /// <summary> Number of missed boosters </summary>
            public int Missed { get; set; } = 0;
            /// <summary> Number of missed boosters that have been acknowledged by the player (displays missed boosters popup ingame if unequal with <see cref="Missed"/>) </summary>
            public int MissedAck { get; set; } = 0;

            public CustomDropServerBoosterImplantInventoryItem[] Inventory { get; set; } = new CustomDropServerBoosterImplantInventoryItem[0];

            public CustomCategory() { }

            public CustomCategory(BoosterImplantCategory cat)
            {
                CategoryType = cat;
            }

            [JsonIgnore]
            public static int MaxBoostersInCategoryInventory
            {
                get
                {
                    return GetMaxBoostersInCategory();
                }
            }

            [JsonIgnore]
            public bool InventoryIsFull
            {
                get
                {
                    return Inventory.Length >= MaxBoostersInCategoryInventory;
                }
            }

            public uint[] GetUsedIds()
            {
                uint[] usedIds = new uint[Inventory.Length];
                for (int i = 0; i < Inventory.Length; i++)
                {
                    usedIds[i] = Inventory[i].InstanceId;
                }
                return usedIds;
            }

            /// <summary>
            /// Add a Booster into this categories inventory.
            /// </summary>
            /// <param name="newBooster"></param>
            /// <returns>true if the booster has been added</returns>
            public bool TryAddBooster(CustomDropServerBoosterImplantInventoryItem newBooster)
            {
                if (InventoryIsFull) return false;

                var newInventory = new CustomDropServerBoosterImplantInventoryItem[Inventory.Length + 1];

                for (int i = 0; i < Inventory.Length; i++)
                {
                    newInventory[i] = Inventory[i];
                }

                newInventory[newInventory.Length - 1] = newBooster;

                Inventory = newInventory;

                return true;
            }

            internal void ConsumeOrDropBoostersWithIds(uint[] boostersToBeConsumed)
            {
                var newInventory = new List<CustomDropServerBoosterImplantInventoryItem>();

                foreach(var item in Inventory)
                {
                    if (boostersToBeConsumed.Any(toConsumeId => item.InstanceId == toConsumeId))
                    {
                        if (item.Uses > 1)
                            item.Uses -= 1; // consumes one charge
                        else
                            continue; // removes the Booster on last charge
                    }

                    newInventory.Add(item);
                }

                Inventory = newInventory.ToArray();
            }

            internal void DropBoostersWithIds(uint[] boostersToBeDropped)
            {
                var newInventory = new List<CustomDropServerBoosterImplantInventoryItem>();

                foreach (var item in Inventory)
                {
                    if (boostersToBeDropped.Any(toDropId => item.InstanceId == toDropId))
                    {
                        continue;
                    }

                    newInventory.Add(item);
                }

                Inventory = newInventory.ToArray();
            }

            internal void SetBoostersTouchedWithIds(uint[] boostersThatWereTouched)
            {
                foreach (var item in Inventory)
                {
                    if (boostersThatWereTouched.Any(toTouchId => item.InstanceId == toTouchId))
                    {
                        item.IsTouched = true;
                    }
                }
            }

            public object ToBaseGame() => ToBaseGame(this);

            public static object ToBaseGame(CustomCategory customCat)
            {
                return ImplementationInstanceManager.ToBaseGameConverter(customCat);
            }

            public static CustomCategory FromBaseGame(object baseGame)
            {
                return ImplementationInstanceManager.FromBaseGameConverter<CustomCategory>(baseGame);
            }

            public static int GetMaxBoostersInCategory()
            {
                var current = ArchiveMod.CurrentRundown;

                if (current <= Utils.RundownID.RundownFive)
                    return MAX_BOOSTERS_R5;

                if (current >= Utils.RundownID.RundownSix)
                    return MAX_BOOSTERS_R6;

                ArchiveLogger.Warning($"Unknown booster inventory size for {current}, defaulting to {MAX_BOOSTERS_R6}!");
                return MAX_BOOSTERS_R6;
            }
        }
    }
}
