using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Editor.Viewport;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Characters;
using ZoneReborn.World.Characters.Locomotion;

namespace Editor.Characters;

public sealed class CharacterEditorPanel : UserControl
{
    private readonly ComboBox _category = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly TextBox _search = new() { Watermark = "Название вещи или номер…" };
    private readonly ListBox _catalogList = new() { MaxHeight = 210, MinHeight = 90 };
    private readonly ListBox _equippedList = new() { MaxHeight = 180, MinHeight = 70 };
    private readonly TextBlock _summary = Text("Откройте уровень для просмотра персонажа.");
    private readonly TextBlock _details = Text("Выберите вещь в каталоге.");
    private readonly TextBlock _status = Text("");
    private readonly Button _show = Button("Показать персонажа");
    private readonly Button _equip = Button("Надеть");
    private readonly Button _remove = Button("Снять выбранное");
    private readonly Button _reset = Button("Стандартный комплект");
    private readonly Button _drawWeapon = Button("Взять оружие в руки");
    private readonly Button _holsterWeapon = Button("Убрать оружие");
    private readonly TextBlock _weaponState = Text("Оружие не надето.");
    private CharacterEquipmentCatalog? _catalog;
    private DxViewportHost? _viewport;
    private AssetPaths? _assets;
    private CancellationTokenSource? _session;
    private bool _binding;

    public bool IsActive { get; private set; }
    public bool IsBusy { get; private set; }
    public event Action? BusyChanged;

    public CharacterEditorPanel()
    {
        var root = new StackPanel { Spacing = 8 };
        root.Children.Add(_summary);
        root.Children.Add(_show);
        root.Children.Add(Text("ПКМ + мышь — осмотр · колесо — расстояние"));
        root.Children.Add(Title("КАТАЛОГ ЭКИПИРОВКИ"));
        root.Children.Add(_category);
        root.Children.Add(_search);
        root.Children.Add(_catalogList);
        root.Children.Add(_details);
        root.Children.Add(_equip);
        root.Children.Add(Title("НАДЕТО НА ПЕРСОНАЖЕ"));
        root.Children.Add(_equippedList);
        root.Children.Add(_weaponState);
        root.Children.Add(_drawWeapon);
        root.Children.Add(_holsterWeapon);
        root.Children.Add(_remove);
        root.Children.Add(_reset);
        root.Children.Add(Text("В Play: H — достать / убрать оружие. Комплект сохраняется для следующего Play."));
        root.Children.Add(_status);
        Content = root;
        _catalogList.ItemTemplate = ItemTemplate(false);
        _equippedList.ItemTemplate = ItemTemplate(true);
        _category.SelectionChanged += (_, _) => { if (!_binding) Filter(); };
        _search.TextChanged += (_, _) => Filter();
        _catalogList.SelectionChanged += (_, _) => SelectionChanged();
        _equippedList.SelectionChanged += (_, _) => RefreshState();
        _show.Click += async (_, _) => await ActivateAsync();
        _equip.Click += async (_, _) =>
        {
            if (_catalogList.SelectedItem is CharacterEquipmentItem item && _catalog != null
                && _viewport?.CurrentCharacterOutfit is { } outfit)
                await ApplyAsync(_catalog.Equip(outfit, item));
        };
        _remove.Click += async (_, _) =>
        {
            if (_equippedList.SelectedItem is CharacterEquipmentItem item && _catalog != null
                && _viewport?.CurrentCharacterOutfit is { } outfit)
                await ApplyAsync(_catalog.Remove(outfit, item.TypeId));
        };
        _reset.Click += async (_, _) =>
        {
            if (_catalog != null) await ApplyAsync(_catalog.DefaultOutfit);
        };
        _drawWeapon.Click += (_, _) => _viewport?.SetCharacterWeaponInHands(true);
        _holsterWeapon.Click += (_, _) => _viewport?.SetCharacterWeaponInHands(false);
        RefreshState();
    }

    public void Attach(DxViewportHost viewport, AssetPaths assets)
    {
        _viewport = viewport;
        _assets = assets;
        viewport.PlayStateChanged += RefreshState;
        viewport.LevelChanged += RefreshState;
        viewport.CharacterWeaponChanged += RefreshState;
    }

    public void SetActive(bool active)
    {
        if (active == IsActive) return;
        IsActive = active;
        if (active) _session = new CancellationTokenSource();
        else
        {
            _session?.Cancel();
            _session?.Dispose();
            _session = null;
            IsBusy = false;
            _viewport?.ExitCharacterEditor();
            BusyChanged?.Invoke();
        }
        RefreshState();
    }

    public async Task ActivateAsync()
    {
        await RunAsync(async token =>
        {
            if (_viewport == null || _assets == null) return;
            _status.Text = "Чтение каталога одежды и оружия…";
            _catalog = await _viewport.LoadCharacterCatalogAsync(_assets, token);
            token.ThrowIfCancellationRequested();
            BindCatalog();
            if (!_viewport.HasLoadedLevel)
            {
                _status.Text = "Каталог готов. Откройте уровень и нажмите «Показать персонажа».";
                return;
            }
            _status.Text = "Загрузка персонажа на уровне…";
            await _viewport.EnterCharacterEditorAsync(_assets, token);
            _status.Text = _viewport.CharacterEquipmentWarning ?? "Выберите вещь и нажмите «Надеть».";
        });
    }

    private async Task ApplyAsync(CharacterOutfit outfit)
    {
        await RunAsync(async token =>
        {
            if (_viewport == null) return;
            _status.Text = "Обновление комплекта…";
            await _viewport.ApplyCharacterOutfitAsync(outfit, token);
            _status.Text = "Комплект применён и сохранён.";
        });
    }

    private async Task RunAsync(Func<CancellationToken, Task> operation)
    {
        if (!IsActive || IsBusy || _session == null) return;
        var session = _session;
        var token = session.Token;
        IsBusy = true;
        RefreshState();
        BusyChanged?.Invoke();
        try { await operation(token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (OperationCanceledException)
        {
            if (ReferenceEquals(_session, session)) _status.Text = "Операция отменена.";
        }
        catch (Exception exception)
        {
            if (ReferenceEquals(_session, session)) _status.Text = exception.Message;
        }
        finally
        {
            if (ReferenceEquals(_session, session))
            {
                IsBusy = false;
                RefreshEquipped();
                RefreshState();
                BusyChanged?.Invoke();
            }
        }
    }

    private void BindCatalog()
    {
        if (_catalog == null) return;
        string? category = _category.SelectedItem as string;
        var categories = new[] { "Все вещи" }.Concat(_catalog.Items.Select(i => i.Category)
            .Distinct().Order(StringComparer.CurrentCultureIgnoreCase)).ToArray();
        _binding = true;
        _category.ItemsSource = categories;
        _category.SelectedItem = category != null && categories.Contains(category) ? category : categories[0];
        _binding = false;
        Filter();
        RefreshEquipped();
    }

    private void Filter()
    {
        string query = _search.Text?.Trim() ?? "";
        string? category = _category.SelectedItem as string;
        var selected = _catalogList.SelectedItem;
        var items = _catalog?.Items.Where(i => (category == null || category == "Все вещи" || i.Category == category)
            && (query.Length == 0 || i.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || i.TypeId.ToString().Contains(query, StringComparison.Ordinal)))
            .OrderBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase).ToArray() ?? [];
        _catalogList.ItemsSource = items;
        if (selected is CharacterEquipmentItem item && items.Contains(item)) _catalogList.SelectedItem = selected;
        SelectionChanged();
    }

    private void SelectionChanged()
    {
        _details.Text = _catalogList.SelectedItem is CharacterEquipmentItem item
            ? $"{item.Name}\n{item.Category} · #{item.TypeId}" + (item.Weapon is { } weapon
                ? $"\n{weapon.TypeName} · тип {weapon.GunType}\n{(weapon.WeaponSlot == 2 ? "Кобура" : "Крепление на спине")}" : "")
            : "Выберите вещь в каталоге.";
        RefreshState();
    }

    private void RefreshEquipped()
    {
        if (_catalog == null || _viewport?.CurrentCharacterOutfit is not { } outfit) return;
        var selected = _equippedList.SelectedItem as CharacterEquipmentItem;
        var ids = outfit.ItemTypes.ToHashSet();
        if (outfit.WeaponType is { } weapon) ids.Add(weapon);
        var items = _catalog.Items.Where(i => ids.Contains(i.TypeId)).OrderBy(i => i.Category).ToArray();
        _equippedList.ItemsSource = items;
        if (selected != null) _equippedList.SelectedItem = items.FirstOrDefault(i => i.TypeId == selected.TypeId);
    }

    private void RefreshState()
    {
        bool viewing = _viewport?.IsCharacterEditing == true && _viewport.IsPlaying;
        var state = _viewport?.CurrentCharacterWeaponState;
        bool requestedHeld = _viewport?.CharacterWeaponRequestedHeld == true;
        bool weaponChanging = state != null && (state is not (CharacterWeaponState.Holstered or CharacterWeaponState.Held)
            || requestedHeld != (state == CharacterWeaponState.Held));
        _summary.Text = viewing ? "Персонаж на открытом уровне. Выбирайте экипировку справа."
            : _viewport?.HasLoadedLevel == true ? "Покажите персонажа на текущем уровне."
            : "Откройте уровень для просмотра персонажа.";
        _show.IsEnabled = IsActive && !IsBusy && !viewing && _viewport?.HasLoadedLevel == true;
        _equip.IsEnabled = viewing && !IsBusy && !weaponChanging && _catalogList.SelectedItem is CharacterEquipmentItem;
        _remove.IsEnabled = viewing && !IsBusy && !weaponChanging && _equippedList.SelectedItem is CharacterEquipmentItem;
        _reset.IsEnabled = viewing && !IsBusy && !weaponChanging && _catalog != null;
        _drawWeapon.IsEnabled = viewing && !IsBusy && state == CharacterWeaponState.Holstered && !requestedHeld;
        _holsterWeapon.IsEnabled = viewing && !IsBusy && state == CharacterWeaponState.Held && requestedHeld;
        _weaponState.Text = state switch
        {
            CharacterWeaponState.Holstered => requestedHeld ? "Подготовка Equip…" : "Оружие убрано.",
            CharacterWeaponState.EquippingIn or CharacterWeaponState.EquippingOut => "Equip — достаёт оружие…",
            CharacterWeaponState.Held => requestedHeld ? "Оружие в руках." : "Подготовка Dequip…",
            CharacterWeaponState.HolsteringIn or CharacterWeaponState.HolsteringOut => "Dequip — убирает оружие…",
            _ => "Оружие не надето. Выберите его в каталоге."
        };
        _category.IsEnabled = !IsBusy;
        _search.IsEnabled = !IsBusy;
        _catalogList.IsEnabled = !IsBusy;
        _equippedList.IsEnabled = !IsBusy;
    }

    private static FuncDataTemplate<CharacterEquipmentItem> ItemTemplate(bool equipped) => new((item, _) =>
        Text(item == null ? "" : equipped ? $"{item.Name}\n{item.Category}" : item.Name));
    private static TextBlock Text(string text) => new() { Text = text, FontSize = 12, TextWrapping = TextWrapping.Wrap };
    private static TextBlock Title(string text) => new() { Text = text, FontSize = 11, FontWeight = FontWeight.Bold };
    private static Button Button(string text) => new() { Content = text, HorizontalAlignment = HorizontalAlignment.Stretch };
}
