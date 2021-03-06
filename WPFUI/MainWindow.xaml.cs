﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Engine.EventArgs;
using Engine.Models;
using Engine.Services;
using Engine.ViewModels;

namespace WPFUI
{
    public partial class MainWindow : Window
    {
        private readonly MessageBroker _messageBroker = MessageBroker.GetInstance();
        private readonly GameSession _gameSession;
        private readonly Dictionary<Key, Action> _userInputActions =
            new Dictionary<Key, Action>();

        public MainWindow()
        {
            InitializeComponent();

            InitializeUserInputActions();

            _messageBroker.OnMessageRaised += OnGameMessageRaised;

            _gameSession = SaveGameService.LoadLastSaveOrCreateNew();

            DataContext = _gameSession;
        }

        private void OnButton_MoveNorth(object sender, RoutedEventArgs e)
            => _gameSession.MoveNorth();

        private void OnButton_MoveWest(object sender, RoutedEventArgs e)
            => _gameSession.MoveWest();

        private void OnButton_MoveEast(object sender, RoutedEventArgs e)
            => _gameSession.MoveEast();

        private void OnButton_MoveSouth(object sender, RoutedEventArgs e)
            => _gameSession.MoveSouth();

        private void OnClick_AttackMonster(object sender, RoutedEventArgs e)
            => _gameSession.AttackCurrentMonster();

        private void OnClick_UseCurrentConsumable(object sender, RoutedEventArgs e)
            => _gameSession.UseCurrentConsumable();

        private void OnGameMessageRaised(object sender, GameMessageEventArgs e)
        {
            GameMessages.Document.Blocks.Add(new Paragraph(new Run(e.Message)));
            GameMessages.ScrollToEnd();
        }

        private void OnClick_DisplayTradeScreen(object sender, RoutedEventArgs e)
        {
            if(_gameSession.CurrentTrader != null) {
                TradeScreen tradeScreen = new TradeScreen();
                tradeScreen.Owner = this;
                tradeScreen.DataContext = _gameSession;
                tradeScreen.ShowDialog();
            }
        }

        private void OnClick_Craft(object sender, RoutedEventArgs e)
        {
            Recipe recipe = ((FrameworkElement)sender).DataContext as Recipe;
            _gameSession.CraftItemUsing(recipe);
        }

        private void InitializeUserInputActions()
        {
            // move north
            _userInputActions.Add(Key.W, () => _gameSession.MoveNorth());
            _userInputActions.Add(Key.Up, () => _gameSession.MoveNorth());
            // move west
            _userInputActions.Add(Key.A, () => _gameSession.MoveWest());
            _userInputActions.Add(Key.Left, () => _gameSession.MoveWest());
            // move south
            _userInputActions.Add(Key.S, () => _gameSession.MoveSouth());
            _userInputActions.Add(Key.Down, () => _gameSession.MoveSouth());
            // move east
            _userInputActions.Add(Key.D, () => _gameSession.MoveEast());
            _userInputActions.Add(Key.Right, () => _gameSession.MoveEast());
            // attack
            _userInputActions.Add(Key.Z, () => _gameSession.AttackCurrentMonster());
            // consume
            _userInputActions.Add(Key.C, () => _gameSession.UseCurrentConsumable());

            _userInputActions.Add(Key.I, () => SetTabFocusTo("InventoryTabItem"));
            _userInputActions.Add(Key.Q, () => SetTabFocusTo("QuestsTabItem"));
            _userInputActions.Add(Key.R, () => SetTabFocusTo("RecipesTabItem"));
            _userInputActions.Add(Key.T, () => OnClick_DisplayTradeScreen(this, new RoutedEventArgs()));

            // exit game
            _userInputActions.Add(Key.Escape, () => Close());
        }

        private void MainWindow_OnKeyDown(object sender, KeyEventArgs e)
        {
            if(_userInputActions.ContainsKey(e.Key)) {
                _userInputActions[e.Key].Invoke();
            }
        }

        private void SetTabFocusTo(string tabName)
        {
            foreach(object item in PlayerDataTabControl.Items) {
                if(item is TabItem tabItem) {
                    if(tabItem.Name == tabName) {
                        tabItem.IsSelected = true;
                        return;
                    }
                }
            }
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
            => SaveGameService.Save(_gameSession);
    }
}
