using System.Collections.ObjectModel;
using TaskTracker.Models;
using TaskTracker.Services;

namespace TaskTracker;

public partial class MetersPage : ContentPage
{
    private readonly MeterRepository _repository;
    private readonly List<Meter> _allMeters = new();

    public ObservableCollection<Meter> Meters { get; } = new();

    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged();
            ApplyFilters();
        }
    }

    private string _searchText = string.Empty;

    public MetersPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        _repository = services?.GetService<MeterRepository>() ?? new MeterRepository();

        BindingContext = this;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadMeters();
    }

    private void LoadMeters()
    {
        _allMeters.Clear();
        _allMeters.AddRange(_repository.GetAll());
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        IEnumerable<Meter> query = _allMeters;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(meter =>
                meter.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || meter.Number.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        Meters.Clear();
        foreach (var meter in query.OrderBy(meter => meter.Number).ThenBy(meter => meter.Name))
        {
            Meters.Add(meter);
        }

    }

    private async void OnMeterSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Meter meter)
        {
            return;
        }

        ((CollectionView)sender!).SelectedItem = null;
        await Shell.Current.GoToAsync($"meterdetail?meterId={meter.Id}");
    }

    private async void OnNewMeterClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("newmeter");
    }
}
