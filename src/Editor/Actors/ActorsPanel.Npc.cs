using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ZoneReborn.World.Actors;

namespace Editor.Actors
{
    public sealed partial class ActorsPanel
    {
        private ActorNpcCatalog? _npcCatalog;

        private void BuildNpcProperties(LevelActor actor)
        {
            _properties.Children.Add(new Separator());

            _properties.Children.Add(new TextBlock
            {
                Text = "Настройки NPC",
                FontSize = 15,
                FontWeight = FontWeight.SemiBold
            });

            try
            {
                if (_npcCatalog == null && _assets != null)
                {
                    _npcCatalog = ActorNpcCatalog.Read(_assets);
                }
            }
            catch (Exception exception)
            {
                _properties.Children.Add(new TextBlock
                {
                    Text = $"Ошибка каталога NPC: {exception.Message}",
                    TextWrapping = TextWrapping.Wrap
                });
            }

            if (_npcCatalog != null)
            {
                BuildNpcSelector(actor, _npcCatalog);
            }

            AddText(
                _properties,
                "Template ID",
                actor.TemplateId,
                value =>
                {
                    if (actor.TemplateId == value)
                    {
                        return;
                    }

                    actor.TemplateId = value;
                    actor.NpcId = 0;
                    actor.NpcHeadPreset = "";
                    actor.NpcBarterList = "";
                    actor.NpcFactionId = 0;

                    Changed();
                });

            _properties.Children.Add(new TextBlock
            {
                Text = $"NPC ID: {actor.NpcId}"
            });

            AddText(
                _properties,
                "Head Preset",
                actor.NpcHeadPreset,
                value =>
                {
                    actor.NpcHeadPreset = value;
                    Changed();
                });

            AddNumber(
                _properties,
                "Faction ID",
                actor.NpcFactionId,
                0,
                int.MaxValue,
                1,
                value =>
                {
                    actor.NpcFactionId = (int)value;
                    Changed();
                });

            _properties.Children.Add(new Separator());

            _properties.Children.Add(new TextBlock
            {
                Text = "Возможности NPC",
                FontWeight = FontWeight.SemiBold
            });

            AddNpcFlag(
                actor,
                "Диалог",
                NpcCapabilities.CanTalk);

            AddNpcFlag(
                actor,
                "Торговля",
                NpcCapabilities.CanTrade);

            AddNpcFlag(
                actor,
                "Выдача квестов",
                NpcCapabilities.QuestGiver);

            AddNpcFlag(
                actor,
                "Подсветка ночью",
                NpcCapabilities.NightHighlight);

            AddText(
                _properties,
                "Barter List",
                actor.NpcBarterList,
                value =>
                {
                    actor.NpcBarterList = value;
                    Changed();
                });
        }

        private void BuildNpcSelector(
            LevelActor actor,
            ActorNpcCatalog catalog)
        {
            var filter = new ComboBox
            {
                ItemsSource = new[]
                {
                    "Все NPC",
                    "Торговые профили",
                    "NPC с автоквестами",
                    "Медики",
                    "Оружейники",
                    "Ремонтники",
                    "Проводники",
                    "Бармены"
                },
                SelectedIndex = 0,
                HorizontalAlignment =
                    HorizontalAlignment.Stretch
            };

            var search = new TextBox
            {
                PlaceholderText = "Имя, Template ID или NPC ID..."
            };

            var npcSelector = new ComboBox
            {
                HorizontalAlignment =
                    HorizontalAlignment.Stretch,

                MaxDropDownHeight = 350
            };

            var information = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap
            };

            var resultCount = new TextBlock();

            void RefreshNpcList()
            {
                string query = search.Text?.Trim() ?? "";

                var items = catalog.Items
                    .Where(item => MatchesNpcFilter(
                        item,
                        filter.SelectedIndex))
                    .Where(item =>
                        string.IsNullOrWhiteSpace(query) ||
                        item.Name.Contains(
                            query,
                            StringComparison.OrdinalIgnoreCase) ||
                        item.TemplateId.Contains(
                            query,
                            StringComparison.OrdinalIgnoreCase) ||
                        item.NpcId.ToString().Contains(query))
                    .ToArray();

                npcSelector.ItemsSource = items;

                npcSelector.SelectedItem =
                    items.FirstOrDefault(item =>
                        item.TemplateId == actor.TemplateId);

                resultCount.Text =
                    $"Найдено NPC: {items.Length}";
            }

            filter.SelectionChanged += (_, _) =>
                RefreshNpcList();

            search.TextChanged += (_, _) =>
                RefreshNpcList();

            npcSelector.SelectionChanged += (_, _) =>
            {
                if (npcSelector.SelectedItem
                    is not ActorNpcDefinition npc)
                {
                    information.Text = "";
                    return;
                }

                information.Text =
                    $"NPC ID: {npc.NpcId}\n" +
                    $"Template: {npc.TemplateId}\n" +
                    $"Профессия: {npc.Profession}\n" +
                    $"Фракция SO: {npc.FactionId}\n" +
                    $"Локация SO: {npc.SourceLocation}\n" +
                    $"Head Preset: {npc.HeadPreset}\n" +
                    $"Barter List: {npc.BarterList}";
            };

            AddField(_properties, "Категория NPC", filter);
            AddField(_properties, "Поиск NPC", search);
            AddField(_properties, "Выбор NPC", npcSelector);

            _properties.Children.Add(resultCount);
            _properties.Children.Add(information);

            _properties.Children.Add(CreateButton(
                "Применить выбранного NPC",
                () =>
                {
                    if (npcSelector.SelectedItem
                        is not ActorNpcDefinition npc)
                    {
                        return;
                    }

                    actor.NpcId = npc.NpcId;
                    actor.TemplateId = npc.TemplateId;
                    actor.Name = npc.Name;
                    actor.NpcHeadPreset = npc.HeadPreset;
                    actor.NpcBarterList = npc.BarterList;
                    actor.NpcFactionId = npc.FactionId;

                    Changed();
                    BuildProperties();
                }));

            RefreshNpcList();
        }

        private static bool MatchesNpcFilter(
            ActorNpcDefinition npc,
            int filter)
        {
            return filter switch
            {
                1 => npc.IsTradeCandidate,

                2 => npc.HasAutoQuests,

                3 => npc.Profession == "DOCTOR",

                4 => npc.Profession == "WEAPONER",

                5 => npc.Profession is
                    "ENGINEER" or "ARMORER" ||
                    npc.TemplateId.Contains(
                        "repair",
                        StringComparison.OrdinalIgnoreCase),

                6 => npc.Profession is
                    "GUIDE" or "OC_Guide",

                7 => npc.Profession == "BARMAN",

                _ => true
            };
        }

        private void AddNpcFlag(
            LevelActor actor,
            string title,
            NpcCapabilities flag)
        {
            var checkBox = new CheckBox
            {
                Content = title,
                IsChecked = actor.NpcFlags.HasFlag(flag)
            };

            checkBox.IsCheckedChanged += (_, _) =>
            {
                if (checkBox.IsChecked == true)
                {
                    actor.NpcFlags |= flag;
                }
                else
                {
                    actor.NpcFlags &= ~flag;
                }

                Changed();
            };

            _properties.Children.Add(checkBox);
        }
    }
}
