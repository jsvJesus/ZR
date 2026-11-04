using System.IO;
using Editor.Characters;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Characters;
using ZoneReborn.World.Characters.Locomotion;

namespace Editor.Viewport;

public sealed partial class DxViewportHost
{
    private Task<CharacterEquipmentCatalog>? _characterCatalogLoad;
    private CharacterEquipmentCatalog? _characterCatalog;
    private CharacterOutfit? _characterOutfit;
    private CharacterAsset? _characterAsset;
    private CancellationTokenSource? _characterChange;
    private int _characterEditorVersion;
    private bool _characterEditorOwnsPlay;
    private CharacterWeaponState? _lastCharacterWeaponState;

    public bool IsCharacterEditing { get; private set; }
    public bool HasLoadedLevel => _level != null;
    public CharacterOutfit? CurrentCharacterOutfit => _characterOutfit;
    public string? CharacterEquipmentWarning { get; private set; }
    public event Action? LevelChanged;
    public event Action? CharacterWeaponChanged;
    public CharacterWeaponState? CurrentCharacterWeaponState => _player?.Weapon is { Definition: not null } weapon ? weapon.State : null;
    public bool CharacterWeaponRequestedHeld => _player?.Weapon.RequestedHeld == true;

    public void SetCharacterWeaponInHands(bool held)
    {
        if (_characterChange != null || _player?.Weapon.Definition == null) return;
        _player.Weapon.Request(held);
        CharacterWeaponChanged?.Invoke();
    }

    private void PublishCharacterWeaponState()
    {
        var state = CurrentCharacterWeaponState;
        if (state == _lastCharacterWeaponState) return;
        _lastCharacterWeaponState = state;
        CharacterWeaponChanged?.Invoke();
    }

    public async Task<CharacterEquipmentCatalog> LoadCharacterCatalogAsync(AssetPaths assets, CancellationToken token)
    {
        if (_characterCatalog != null) return _characterCatalog;
        var loading = _characterCatalogLoad ??= Task.Run(() => CharacterEquipmentCatalog.Read(assets, CancellationToken.None));
        CharacterEquipmentCatalog catalog;
        try { catalog = await loading.WaitAsync(token); }
        catch
        {
            if (loading.IsFaulted && ReferenceEquals(_characterCatalogLoad, loading)) _characterCatalogLoad = null;
            throw;
        }
        token.ThrowIfCancellationRequested();
        if (_characterCatalog != null) return _characterCatalog;
        var outfit = catalog.DefaultOutfit;
        try
        {
            outfit = CharacterOutfitStore.Read(assets) ?? outfit;
            catalog.Compose(outfit);
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException
            or System.Text.Json.JsonException or ArgumentException or UnauthorizedAccessException)
        {
            outfit = catalog.DefaultOutfit;
            CharacterEquipmentWarning = $"Сохранённый комплект не загружен: {exception.Message} Использован стандартный.";
        }
        _characterOutfit = outfit;
        _characterCatalog = catalog;
        return catalog;
    }

    public async Task EnterCharacterEditorAsync(AssetPaths assets, CancellationToken token)
    {
        if (IsCharacterEditing && IsPlaying) return;
        if (IsStarting) throw new InvalidOperationException("Дождитесь завершения загрузки Play.");
        if (!HasLoadedLevel) throw new InvalidOperationException("Сначала откройте уровень.");
        SetPlayInspection(false);
        int version = ++_characterEditorVersion;
        _characterEditorOwnsPlay = !IsPlaying;
        IsCharacterEditing = true;
        ReleasePlayMouse(true);
        _pressedKeys.Clear();
        ReleasePlayerInput();
        PlayStateChanged?.Invoke();
        try
        {
            await LoadCharacterCatalogAsync(assets, token);
            token.ThrowIfCancellationRequested();
            if (version != _characterEditorVersion) throw new OperationCanceledException(token);
            if (!IsPlaying) await StartPlayAsync();
            token.ThrowIfCancellationRequested();
            if (version != _characterEditorVersion || !IsPlaying)
                throw new OperationCanceledException(token);
        }
        catch
        {
            if (version == _characterEditorVersion) ExitCharacterEditor();
            throw;
        }
    }

    public void ExitCharacterEditor()
    {
        ++_characterEditorVersion;
        _characterChange?.Cancel();
        bool stop = IsCharacterEditing && _characterEditorOwnsPlay;
        IsCharacterEditing = false;
        _characterEditorOwnsPlay = false;
        _pressedKeys.Clear();
        ReleasePlayerInput();
        if (_rotating) { _rotating = false; ReleaseCapture(); }
        if (stop) StopPlay();
        else PlayStateChanged?.Invoke();
    }

    public async Task ApplyCharacterOutfitAsync(CharacterOutfit outfit, CancellationToken token)
    {
        if (!IsCharacterEditing || _player == null || _characterAsset == null || _surface == null)
            throw new InvalidOperationException("Сначала покажите персонажа в Character Editor.");
        var assets = _assets ?? throw new InvalidOperationException("Ресурсы не найдены.");
        var renderer = _renderer ?? throw new InvalidOperationException("Viewport ещё не готов.");
        var catalog = _characterCatalog ?? throw new InvalidOperationException("Каталог не загружен.");
        var template = _characterAsset;
        var player = _player;
        var position = player.Position;
        float yaw = player.Yaw;
        var surface = _surface;
        var level = _level;
        int version = _characterEditorVersion;
        _characterChange?.Cancel();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        _characterChange = cancellation;
        try
        {
            bool wantedHeld = player.Weapon.RequestedHeld;
            bool sameWeapon = template.Weapon?.TypeId == outfit.WeaponType;
            // Changing the selected model must let the old weapon finish returning to its holster.
            if (!sameWeapon && player.Weapon.Definition != null)
            {
                player.Weapon.Request(false);
                await WaitForWeaponHolsterAsync(player, cancellation.Token);
            }
            position = player.Position;
            yaw = player.Yaw;
            var parts = catalog.Compose(outfit);
            var loaded = await Task.Run(() =>
            {
                var asset = CharacterLoader.Rebuild(assets, parts, template, cancellation.Token);
                var replacement = new CharacterLocomotion(asset, surface, position, yaw);
                if (wantedHeld)
                {
                    if (sameWeapon) replacement.RestoreWeaponInHands();
                    else replacement.Weapon.Request(true);
                }
                cancellation.Token.ThrowIfCancellationRequested();
                return (Asset: asset, Player: replacement);
            }, cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            if (version != _characterEditorVersion || !IsCharacterEditing || !ReferenceEquals(_level, level)
                || !ReferenceEquals(_renderer, renderer) || !ReferenceEquals(_player, player))
                throw new OperationCanceledException(cancellation.Token);
            // SetCharacter constructs GPU resources before releasing the previous model.
            renderer.SetCharacter(assets, loaded.Asset, loaded.Player);
            _characterAsset = loaded.Asset;
            _player = loaded.Player;
            _characterOutfit = outfit;
            PublishCharacterWeaponState();
            try { CharacterOutfitStore.Write(assets, outfit); }
            catch (Exception exception) when (exception is System.IO.IOException or UnauthorizedAccessException)
            {
                throw new InvalidOperationException($"Комплект применён, но не сохранён: {exception.Message}", exception);
            }
        }
        finally
        {
            if (ReferenceEquals(_characterChange, cancellation)) _characterChange = null;
        }
    }

    private async Task WaitForWeaponHolsterAsync(CharacterLocomotion player, CancellationToken token)
    {
        if (player.Weapon.State == CharacterWeaponState.Holstered) return;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Changed()
        {
            if (!ReferenceEquals(_player, player)) completion.TrySetCanceled();
            else if (player.Weapon.State == CharacterWeaponState.Holstered) completion.TrySetResult();
        }
        CharacterWeaponChanged += Changed;
        try { Changed(); await completion.Task.WaitAsync(token); }
        finally { CharacterWeaponChanged -= Changed; }
    }
}
