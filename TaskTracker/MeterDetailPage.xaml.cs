using System.Collections.ObjectModel;
using TaskTracker.Models;
using TaskTracker.Services;

namespace TaskTracker;

[QueryProperty(nameof(MeterId), "meterId")]
public partial class MeterDetailPage : ContentPage
{
    private readonly MeterRepository _repository;
    private Meter? _meter;

    public ObservableCollection<Position> Positions { get; } = new();

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

    public string Category
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

    public string NewPositionValue
    {
        get => _newPositionValue;
        set
        {
            _newPositionValue = value;
            OnPropertyChanged();
        }
    }

    public string MeterId
    {
        get => _meterId;
        set
        {
            _meterId = value;
            LoadMeter();
        }
    }

    private string _name = string.Empty;
    private string _number = string.Empty;
    private string _category = string.Empty;
    private string _note = string.Empty;
    private string _newPositionValue = string.Empty;
    private string _meterId = string.Empty;

    public MeterDetailPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        _repository = services?.GetService<MeterRepository>() ?? new MeterRepository();

        BindingContext = this;
    }

    private void LoadMeter()
    {
        Positions.Clear();
        if (!int.TryParse(MeterId, out var id))
        {
            return;
        }

        _meter = _repository.GetById(id);
        if (_meter is null)
        {
            return;
        }

        Name = _meter.Name;
        Number = _meter.Number;
        Category = _meter.Category.ToString();
        Note = _meter.Note;

        foreach (var position in _meter.Positions.OrderByDescending(position => position.AddedAt))
        {
            Positions.Add(position);
        }
    }

    private void OnAddPositionClicked(object? sender, EventArgs e)
    {
        if (_meter is null)
        {
            return;
        }

        if (!double.TryParse(NewPositionValue, out var value))
        {
            return;
        }

        var position = _repository.AddPosition(_meter.Id, value);
        _meter.Positions.Add(position);
        Positions.Insert(0, position);
        NewPositionValue = string.Empty;
    }
}
