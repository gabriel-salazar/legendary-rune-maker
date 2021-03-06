﻿using Legendary_Rune_Maker.Controls;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Data.Providers;
using Legendary_Rune_Maker.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Legendary_Rune_Maker.Pages
{
    /// <summary>
    /// Interaction logic for AutomationPage.xaml
    /// </summary>
    public partial class AutomationPage : Page, IPage
    {
        private IList<ChampionPickerControl> Picks = new List<ChampionPickerControl>(),
                                             Bans = new List<ChampionPickerControl>();
        private IList<SummonerSpellControl> Spells = new List<SummonerSpellControl>();

        private Config Config;

        public AutomationPage()
        {
            Config = Config.Current.Clone();
            InitializeComponent();

            this.DataContext = Config;
        }

        private void PickBan_Changed(object sender, EventArgs e)
        {
            var picker = (ChampionPickerControl)sender;
            int n = (int)picker.Tag;

            var dic = picker.Ban ? Config.ChampionsToBan : Config.ChampionsToPick;
            var key = dic.Keys.ElementAt(n);

            dic[key] = picker.Champion?.ID ?? 0;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await PickSummonerSpellPopup.SelectSpell();
        }

        private void Spell_SpellSelected(object sender, EventArgs e)
        {
            var picker = (SummonerSpellControl)sender;
            int n = (int)picker.Tag;

            Config.SpellsToPick[Config.SpellsToPick.Keys.ElementAt(n / 2)][n % 2] = picker.Spell?.ID ?? 0;
        }

        private void Providers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Config.LockLoadProvider = ((Provider)Providers.SelectedItem).Name;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void GenerateControls()
        {
            GenerateChamps(1, 1, 6, Picks);
            GenerateChamps(2, 1, 6, Bans);
            GenerateSpells(3, 1, 6);

            void GenerateChamps(int row, int startCol, int count, IList<ChampionPickerControl> l)
            {
                for (int col = 0; col < count; col++)
                {
                    var c = new ChampionPickerControl(NavigationService);
                    c.ChampionChanged += PickBan_Changed;
                    c.Ban = l == Bans;
                    c.Tag = col;
                    c.Focusable = true;

                    Table.Children.Add(c);
                    l.Add(c);

                    Grid.SetRow(c, row);
                    Grid.SetColumn(c, startCol + col);
                }
            }

            void GenerateSpells(int row, int startCol, int count)
            {
                for (int col = 0; col < count * 2; col++)
                {
                    var c = new SummonerSpellControl();
                    c.SpellSelected += Spell_SpellSelected;
                    c.Tag = col;
                    c.VerticalAlignment = col % 2 == 0 ? VerticalAlignment.Top : VerticalAlignment.Bottom;
                    c.Focusable = true;

                    Table.Children.Add(c);
                    Spells.Add(c);

                    Grid.SetRow(c, row);
                    Grid.SetColumn(c, startCol + col / 2);
                }
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Don't regenerate controls if coming back from another page
            if (Picks.Count > 0)
                return;

            GenerateControls();

            foreach (var item in Actuator.RuneProviders)
            {
                if (!(item is ClientProvider))
                {
                    if (item.Supports(Provider.Options.RunePages))
                        Providers.Items.Add(item);

                    if (item.Supports(Provider.Options.ItemSets))
                        ItemProviders.Items.Add(item);

                    if (item.Supports(Provider.Options.SkillOrder))
                        SkillProviders.Items.Add(item);


                    if (item.Name == Config.LockLoadProvider)
                        Providers.SelectedItem = item;

                    if (item.Name == Config.ItemSetProvider)
                        ItemProviders.SelectedItem = item;

                    if (item.Name == Config.SkillOrderProvider)
                        SkillProviders.SelectedItem = item;
                }
            }

            int i = 0;
            foreach (var item in Config.ChampionsToPick)
            {
                Picks[i++].Champion = Riot.GetChampion(item.Value);
            }

            i = 0;
            foreach (var item in Config.ChampionsToBan)
            {
                Bans[i++].Champion = Riot.GetChampion(item.Value);
            }

            var spells = await Riot.GetSummonerSpellsAsync();

            i = 0;
            foreach (var item in Config.SpellsToPick)
            {
                Spells[i++].Spell = spells.SingleOrDefault(o => o.ID == item.Value[0]);
                Spells[i++].Spell = spells.SingleOrDefault(o => o.ID == item.Value[1]);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Config.Current = Config;
            Config.Current.Save();
        }

        private void ItemProviders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Config.ItemSetProvider = ((Provider)ItemProviders.SelectedItem).Name;
        }

        private void SkillProviders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Config.SkillOrderProvider = ((Provider)SkillProviders.SelectedItem).Name;
        }

        public Size GetSize() => new Size(this.Width, this.Height);
    }
}
