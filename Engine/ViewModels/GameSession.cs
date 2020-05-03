﻿using Engine.EventArgs;
using Engine.Factories;
using Engine.Models;
using System;
using System.Linq;

namespace Engine.ViewModels
{
    public class GameSession : BaseNotificationClass
    {
        public event EventHandler<GameMessageEventArgs> OnMessageRaised;

        #region Properties

        private Player _currentPlayer;
        private Location _currentLocation;
        private Monster _currentMonster;
        private Trader _currentTrader;

        public World CurrentWorld { get; }

        public Player CurrentPlayer {
            get => _currentPlayer;
            set {
                if(_currentPlayer != null) {
                    _currentPlayer.OnLeveledUp -= OnCurrentPlayerLeveledUp;
                    _currentPlayer.OnKilled -= OnCurrentPlayerKilled;
                }

                _currentPlayer = value;

                if(_currentPlayer != null) {
                    _currentPlayer.OnLeveledUp += OnCurrentPlayerLeveledUp;
                    _currentPlayer.OnKilled += OnCurrentPlayerKilled;
                }
            }
        }
        public Location CurrentLocation {
            get => _currentLocation;
            set {
                _currentLocation = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(HasLocationToNorth));
                OnPropertyChanged(nameof(HasLocationToEast));
                OnPropertyChanged(nameof(HasLocationToWest));
                OnPropertyChanged(nameof(HasLocationToSouth));

                CompleteQuestsAtLocation();
                GivePlayerQuestAtLocation();
                GetMonsterAtLocation();

                CurrentTrader = CurrentLocation.TraderHere;
            }
        }

        public Monster CurrentMonster {
            get => _currentMonster;
            set {
                if(_currentMonster != null) {
                    _currentMonster.OnKilled -= OnCurrentMonsterKilled;
                }
                _currentMonster = value;

                if(_currentMonster != null) {
                    _currentMonster.OnKilled += OnCurrentMonsterKilled;

                    RaiseMessage("");
                    RaiseMessage($"You see a {CurrentMonster.Name} here!");
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(HasMonster));
            }
        }

        public Trader CurrentTrader {
            get => _currentTrader;
            set {
                _currentTrader = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(HasTrader));
            }
        }

        public GameItem CurrentWeapon { get; set; }

        public bool HasLocationToNorth
            => CurrentWorld.LocationAt(CurrentLocation.XCoordinate, CurrentLocation.YCoordinate + 1) != null;

        public bool HasLocationToEast
            => CurrentWorld.LocationAt(CurrentLocation.XCoordinate + 1, CurrentLocation.YCoordinate) != null;

        public bool HasLocationToSouth
            => CurrentWorld.LocationAt(CurrentLocation.XCoordinate, CurrentLocation.YCoordinate - 1) != null;

        public bool HasLocationToWest
            => CurrentWorld.LocationAt(CurrentLocation.XCoordinate - 1, CurrentLocation.YCoordinate) != null;

        public bool HasMonster => CurrentMonster != null;

        public bool HasTrader => CurrentTrader != null;

        #endregion

        public GameSession()
        {
            CurrentPlayer = new Player("James", "Fighter", 0, 10, 10, 1000000);

            if(!CurrentPlayer.Weapons.Any()) {
                CurrentPlayer.AddItemToInventory(ItemFactory.CreateGameItem(1001));
            }

            CurrentWorld = WorldFactory.CreateWorld();

            CurrentLocation = CurrentWorld.LocationAt(0, 0);
        }

        public void MoveNorth()
        {
            if(HasLocationToNorth) {
                CurrentLocation = CurrentWorld.LocationAt(CurrentLocation.XCoordinate, CurrentLocation.YCoordinate + 1);
            }
        }

        public void MoveEast()
        {
            if(HasLocationToEast) {
                CurrentLocation = CurrentWorld.LocationAt(CurrentLocation.XCoordinate + 1, CurrentLocation.YCoordinate);
            }
        }

        public void MoveSouth()
        {
            if(HasLocationToSouth) {
                CurrentLocation = CurrentWorld.LocationAt(CurrentLocation.XCoordinate, CurrentLocation.YCoordinate - 1);
            }
        }

        public void MoveWest()
        {
            if(HasLocationToWest) {
                CurrentLocation = CurrentWorld.LocationAt(CurrentLocation.XCoordinate - 1, CurrentLocation.YCoordinate);
            }
        }

        private void CompleteQuestsAtLocation()
        {
            foreach(var quest in CurrentLocation.QuestsAvailableHere) {
                var questToComplete = CurrentPlayer.Quests.FirstOrDefault(q => q.PlayerQuest.ID == quest.ID && !q.IsCompleted);

                if(questToComplete != null) {
                    if(CurrentPlayer.HasAllTheseItems(quest.ItemsToComplete)) {
                        // Remove the quest completion items from the player's inventory
                        foreach(var itemQuantity in quest.ItemsToComplete) {
                            for(var i = 0; i < itemQuantity.Quantity; i++) {
                                CurrentPlayer.RemoveItemFromInventory(CurrentPlayer.Inventory.First(item => item.ItemTypeID == itemQuantity.ItemID));
                            }
                        }

                        RaiseMessage("");
                        RaiseMessage($"You completed the '{quest.Name}' quest");

                        // Give the player the quest rewards
                        RaiseMessage($"You receive {quest.RewardExperiencePoints} experience points");
                        CurrentPlayer.AddExperience(quest.RewardExperiencePoints);

                        RaiseMessage($"You receive {quest.RewardGold} gold");
                        CurrentPlayer.ReceiveGold(quest.RewardGold);

                        foreach(var itemQuantity in quest.RewardItems) {
                            var rewardItem = ItemFactory.CreateGameItem(itemQuantity.ItemID);

                            RaiseMessage($"You receive a {rewardItem.Name}");
                            CurrentPlayer.AddItemToInventory(rewardItem);
                        }

                        // Mark the Quest as completed
                        questToComplete.IsCompleted = true;
                    }
                }
            }
        }

        private void GivePlayerQuestAtLocation()
        {
            foreach(var quest in CurrentLocation.QuestsAvailableHere) {
                if(!CurrentPlayer.Quests.Any(q => q.PlayerQuest.ID == quest.ID)) {
                    CurrentPlayer.Quests.Add(new QuestStatus(quest));

                    RaiseMessage("");
                    RaiseMessage($"You receive the '{quest.Name}' quest");
                    RaiseMessage(quest.Description);

                    RaiseMessage("Return with:");
                    foreach(var itemQuantity in quest.ItemsToComplete) {
                        RaiseMessage($"   {itemQuantity.Quantity} {ItemFactory.CreateGameItem(itemQuantity.ItemID).Name}");
                    }

                    RaiseMessage("And you will receive:");
                    RaiseMessage($"   {quest.RewardExperiencePoints} experience points");
                    RaiseMessage($"   {quest.RewardGold} gold");
                    foreach(var itemQuantity in quest.RewardItems) {
                        RaiseMessage($"   {itemQuantity.Quantity} {ItemFactory.CreateGameItem(itemQuantity.ItemID).Name}");
                    }
                }
            }
        }

        private void GetMonsterAtLocation() => CurrentMonster = CurrentLocation.GetMonster();

        public void AttackCurrentMonster()
        {
            if(CurrentWeapon == null) {
                RaiseMessage("You must select a weapon, to attack.");
                return;
            }

            // Determine damage to monster
            var damageToMonster = RandomNumberGenerator.NumberBetween(CurrentWeapon.MinimumDamage, CurrentWeapon.MaximumDamage);

            if(damageToMonster == 0) {
                RaiseMessage($"You missed the {CurrentMonster.Name}.");
            }
            else {
                RaiseMessage($"You hit the {CurrentMonster.Name} for {damageToMonster} points.");
                CurrentMonster.TakeDamage(damageToMonster);
            }

            if(CurrentMonster.IsDead) {
                // Get another monster to fight
                GetMonsterAtLocation();
            }
            else {
                // Let the monster attack
                var damageToPlayer = RandomNumberGenerator.NumberBetween(CurrentMonster.MinimumDamage, CurrentMonster.MaximumDamage);

                if(damageToPlayer == 0) {
                    RaiseMessage($"The {CurrentMonster.Name} attacks, but misses you.");
                }
                else {
                    RaiseMessage($"The {CurrentMonster.Name} hit you for {damageToPlayer} points.");
                    CurrentPlayer.TakeDamage(damageToPlayer);
                }
            }
        }

        private void OnCurrentPlayerKilled(object sender, System.EventArgs eventArgs)
        {
            RaiseMessage("");
            if(CurrentMonster != null) {
                RaiseMessage($"The {CurrentMonster.Name} killed you.");
            }
            else {
                RaiseMessage($"{CurrentPlayer.Name} has been killed.");
            }

            CurrentLocation = CurrentWorld.LocationAt(0, -1);
            CurrentPlayer.CompletelyHeal();
        }

        private void OnCurrentMonsterKilled(object sender, System.EventArgs eventArgs)
        {
            RaiseMessage("");
            RaiseMessage($"You defeated the {CurrentMonster.Name}!");

            RaiseMessage($"You receive {CurrentMonster.RewardExperiencePoints} experience points.");
            CurrentPlayer.AddExperience(CurrentMonster.RewardExperiencePoints);

            RaiseMessage($"You receive {CurrentMonster.Gold} gold.");
            CurrentPlayer.ReceiveGold(CurrentMonster.Gold);

            foreach(GameItem gameItem in CurrentMonster.Inventory) {
                RaiseMessage($"You receive one {gameItem.Name}.");
                CurrentPlayer.AddItemToInventory(gameItem);
            }
        }

        private void OnCurrentPlayerLeveledUp(object sender, System.EventArgs eventArgs)
            => RaiseMessage($"You are now level {CurrentPlayer.Level}!");

        private void RaiseMessage(string message)
            => OnMessageRaised?.Invoke(this, new GameMessageEventArgs(message));
    }
}
