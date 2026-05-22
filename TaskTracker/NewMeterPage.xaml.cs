using System.Collections.ObjectModel;
using TaskTracker.Models;
using TaskTracker.Models.Enums;
using TaskTracker.Services;

namespace TaskTracker;

public partial class NewMeterPage : ContentPage
{
    private readonly MeterRepository _repository;

    public ObservableCollection<MeterCategory> Categories { get; } = new();

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged();
        }
    }

    public string Number
    {
        get => _number;
        set
        {
            _number = value;
            OnPropertyChanged();
        }
    }

    public MeterCategory Category
    {
        get => _category;
        set
        {
            _category = value;
            OnPropertyChanged();
        }
    }

    public string Note
    {
        get => _note;
        set
        {
            _note = value;
            OnPropertyChanged();
        }
    }

    public bool IsDeactivated
    {
        get => _isDeactivated;
        set
        {
            _isDeactivated = value;
            OnPropertyChanged();
        }
    }

    private string _name = string.Empty;
    private string _number = string.Empty;
    private MeterCategory _category;
    private string _note = string.Empty;
    private bool _isDeactivated;

    public NewMeterPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        _repository = services?.GetService<MeterRepository>() ?? new MeterRepository();

        foreach (var category in Enum.GetValues<MeterCategory>())
        {
            Categories.Add(category);
        }

        Category = MeterCategory.None;
        BindingContext = this;
    }

    private async void OnSaveMeterClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return;
        }

        var meter = new Meter
        {
            Name = Name.Trim(),
            Number = Number.Trim(),
            Category = Category,
            Note = Note.Trim(),
            IsDeactivated = IsDeactivated
        };

        _repository.Upsert(meter);
        await Shell.Current.GoToAsync("..");
    }
}
