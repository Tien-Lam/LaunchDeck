using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using LaunchDeck.Shared;

namespace LaunchDeck.Companion.Editor;

public class EditorViewModel : INotifyPropertyChanged
{
    private readonly EditorModel _model = new();
    private readonly string _configPath;
    private readonly Action? _onSaved;

    public Func<string, string, bool>? ConfirmAction { get; set; }

    public ObservableCollection<ItemViewModel> Items { get; } = new();

    public EditorViewModel(string configPath, Action? onSaved)
    {
        _configPath = configPath;
        _onSaved = onSaved;

        _model.Load(configPath);
        RebuildItems();

        SaveCommand = new RelayCommand(Save);
        AddExeCommand = new RelayCommand(AddExe);
        AddUrlCommand = new RelayCommand(AddUrl);
        AddStoreCommand = new RelayCommand(AddStore);
        DialogSaveCommand = new RelayCommand(DialogSave);
        DialogCancelCommand = new RelayCommand(DialogCancel);
    }

    public bool IsDirty { get; private set; }

    public string ItemCountText => $"{Items.Count} item{(Items.Count == 1 ? "" : "s")}";

    public ICommand SaveCommand { get; }
    public ICommand AddExeCommand { get; }
    public ICommand AddUrlCommand { get; }
    public ICommand AddStoreCommand { get; }
    public ICommand DialogSaveCommand { get; }
    public ICommand DialogCancelCommand { get; }

    private bool _isDialogOpen;
    public bool IsDialogOpen
    {
        get => _isDialogOpen;
        set { _isDialogOpen = value; OnPropertyChanged(); OnPropertyChanged(nameof(DialogVisibility)); }
    }

    public Visibility DialogVisibility => IsDialogOpen ? Visibility.Visible : Visibility.Collapsed;

    private ItemViewModel? _editingItem;
    private LaunchItemType? _addingType;

    private string _editName = "";
    public string EditName
    {
        get => _editName;
        set { _editName = value; OnPropertyChanged(); }
    }

    private string _editTypeLabel = "";
    public string EditTypeLabel
    {
        get => _editTypeLabel;
        set { _editTypeLabel = value; OnPropertyChanged(); }
    }

    private string _editPath = "";
    public string EditPath
    {
        get => _editPath;
        set { _editPath = value; OnPropertyChanged(); }
    }

    private string _editArgs = "";
    public string EditArgs
    {
        get => _editArgs;
        set { _editArgs = value; OnPropertyChanged(); }
    }

    private string _editIcon = "";
    public string EditIcon
    {
        get => _editIcon;
        set { _editIcon = value; OnPropertyChanged(); }
    }

    private bool _editIsExe;
    public bool EditIsExe
    {
        get => _editIsExe;
        set { _editIsExe = value; OnPropertyChanged(); OnPropertyChanged(nameof(EditArgsVisibility)); OnPropertyChanged(nameof(EditBrowsePathVisibility)); }
    }

    private bool _editIsStore;
    public bool EditIsStore
    {
        get => _editIsStore;
        set { _editIsStore = value; OnPropertyChanged(); OnPropertyChanged(nameof(EditBrowsePathVisibility)); }
    }

    public Visibility EditArgsVisibility => EditIsExe ? Visibility.Visible : Visibility.Collapsed;
    public Visibility EditBrowsePathVisibility => (EditIsExe || EditIsStore) ? Visibility.Visible : Visibility.Collapsed;

    public void Edit(ItemViewModel item)
    {
        _editingItem = item;
        _addingType = null;
        EditName = item.Name;
        EditTypeLabel = item.TypeLabel;
        EditPath = item.Path;
        EditArgs = item.Args;
        EditIcon = item.Icon;
        EditIsExe = item.Config.Type == LaunchItemType.Exe;
        EditIsStore = item.Config.Type == LaunchItemType.Store;
        IsDialogOpen = true;
    }

    private void OpenAddDialog(LaunchItemType type, string defaultName, string defaultPath)
    {
        _editingItem = null;
        _addingType = type;
        EditName = defaultName;
        EditTypeLabel = type.ToString().ToLowerInvariant();
        EditPath = defaultPath;
        EditArgs = "";
        EditIcon = "";
        EditIsExe = type == LaunchItemType.Exe;
        EditIsStore = type == LaunchItemType.Store;
        IsDialogOpen = true;
    }

    public void Delete(ItemViewModel item)
    {
        var index = Items.IndexOf(item);
        if (index < 0) return;
        _model.Remove(index);
        Items.RemoveAt(index);
        OnPropertyChanged(nameof(ItemCountText));
        IsDirty = true;
    }

    public void MoveUp(ItemViewModel item)
    {
        var index = Items.IndexOf(item);
        if (!_model.MoveUp(index)) return;
        Items.Move(index, index - 1);
        IsDirty = true;
    }

    public void MoveDown(ItemViewModel item)
    {
        var index = Items.IndexOf(item);
        if (!_model.MoveDown(index)) return;
        Items.Move(index, index + 1);
        IsDirty = true;
    }

    private void AddExe() => OpenAddDialog(LaunchItemType.Exe, "New App", "");
    private void AddUrl() => OpenAddDialog(LaunchItemType.Url, "New URL", "https://");
    private void AddStore() => OpenAddDialog(LaunchItemType.Store, "New Store App", "");

    private void DialogSave()
    {
        if (_editingItem != null)
        {
            _editingItem.Name = EditName;
            _editingItem.Path = EditPath;
            _editingItem.Args = EditArgs;
            _editingItem.Icon = EditIcon;
        }
        else if (_addingType != null)
        {
            var config = new LaunchItemConfig
            {
                Name = EditName,
                Type = _addingType.Value,
                Path = EditPath,
                Args = string.IsNullOrWhiteSpace(EditArgs) ? null : EditArgs,
                Icon = string.IsNullOrWhiteSpace(EditIcon) ? null : EditIcon
            };
            _model.Items.Add(config);
            _model.SelectedIndex = _model.Items.Count - 1;
            Items.Add(new ItemViewModel(config));
            OnPropertyChanged(nameof(ItemCountText));
        }
        IsDirty = true;
        IsDialogOpen = false;
        _editingItem = null;
        _addingType = null;
    }

    private void DialogCancel()
    {
        IsDialogOpen = false;
        _editingItem = null;
        _addingType = null;
    }

    public List<string> Validate() => _model.Validate();

    private void Save()
    {
        var errors = _model.Validate();
        if (errors.Count > 0)
        {
            var message = string.Join("\n", errors) + "\n\nSave anyway?";
            if (ConfirmAction?.Invoke(message, "Validation warnings") != true)
                return;
        }

        _model.Save(_configPath, _onSaved);
        IsDirty = false;
    }

    private void RebuildItems()
    {
        Items.Clear();
        foreach (var config in _model.Items)
            Items.Add(new ItemViewModel(config));
        OnPropertyChanged(nameof(ItemCountText));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
