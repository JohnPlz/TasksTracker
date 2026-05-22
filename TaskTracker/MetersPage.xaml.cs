using System.Collections.ObjectModel;
using TaskTracker.Models;
using TaskTracker.Services;

namespace TaskTracker;

public partial class MetersPage : ContentPage
{
    private readonly MeterRepository _repository;
    private readonly List<Meter> _allMeters = new();
    private const int PageSize = 20;

    public ObservableCollection<Meter> Meters { get; } = new();
    public ObservableCollection<int> Pages { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

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

    public int CurrentPage
    {
        get => _currentPage;
        private set
        {
            if (_currentPage == value)
            {
                return;
            }

            _currentPage = value;
            OnPropertyChanged();
        }
    }

    public int TotalPages
    {
        get => _totalPages;
        private set
        {
            if (_totalPages == value)
            {
                return;
            }

            _totalPages = value;
            OnPropertyChanged();
        }
    }

    private string _searchText = string.Empty;
    private bool _isLoading;
    private int _currentPage = 1;
    private int _totalPages = 1;

    public MetersPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        _repository = services?.GetService<MeterRepository>() ?? new MeterRepository();

        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadMetersAsync();
    }

    private async Task LoadMetersAsync()
    {
        try
        {
            IsLoading = true;
            var items = await _repository.GetAllAsync();
            _allMeters.Clear();
            _allMeters.AddRange(items);
            ApplyFilters();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters(bool resetPage = true)
    {
        IEnumerable<Meter> query = _allMeters;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(meter =>
                meter.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || meter.Number.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = query.OrderBy(meter => meter.Number).ThenBy(meter => meter.Name).ToList();
        UpdatePaging(ordered.Count, resetPage);
        var paged = ordered
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        Meters.Clear();
        foreach (var meter in paged)
        {
            Meters.Add(meter);
        }
    }

    private void UpdatePaging(int totalCount, bool resetPage)
    {
        var pages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        TotalPages = pages;

        if (resetPage || CurrentPage > TotalPages)
        {
            CurrentPage = 1;
        }

        Pages.Clear();
        for (var i = 1; i <= TotalPages; i++)
        {
            Pages.Add(i);
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

    private void OnPageButtonClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        if (button.CommandParameter is not int page)
        {
            return;
        }

        if (page < 1 || page > TotalPages)
        {
            return;
        }

        CurrentPage = page;
        ApplyFilters(false);
    }
}
