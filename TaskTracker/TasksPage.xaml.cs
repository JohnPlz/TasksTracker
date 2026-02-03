using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using TaskTracker.Models;
using TaskTracker.Services;

namespace TaskTracker;

public partial class TasksPage : ContentPage
{
    private readonly TaskRepository _repository;
    private readonly List<TaskEntry> _allTasks = new();

    public ObservableCollection<TaskEntry> Tasks { get; } = new();

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

    public bool UseDateFilter
    {
        get => _useDateFilter;
        set
        {
            _useDateFilter = value;
            OnPropertyChanged();
            ApplyFilters();
        }
    }

    public DateTime FilterFrom
    {
        get => _filterFrom;
        set
        {
            _filterFrom = value;
            OnPropertyChanged();
            ApplyFilters();
        }
    }

    public DateTime FilterTo
    {
        get => _filterTo;
        set
        {
            _filterTo = value;
            OnPropertyChanged();
            ApplyFilters();
        }
    }

    private string _searchText = string.Empty;
    private bool _useDateFilter;
    private DateTime _filterFrom = DateTime.Today.AddDays(-30);
    private DateTime _filterTo = DateTime.Today;

    public TasksPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        _repository = services?.GetService<TaskRepository>() ?? new TaskRepository();

        BindingContext = this;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadTasks();
    }

    private void LoadTasks()
    {
        _allTasks.Clear();
        _allTasks.AddRange(_repository.GetAll());
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        IEnumerable<TaskEntry> query = _allTasks;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(item => item.Description.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (UseDateFilter)
        {
            var from = FilterFrom.Date;
            var to = FilterTo.Date;
            if (from > to)
            {
                (from, to) = (to, from);
            }

            var toInclusive = to.AddDays(1).AddTicks(-1);
            query = query.Where(item => item.StartTime >= from && item.StartTime <= toInclusive);
        }

        Tasks.Clear();
        foreach (var item in query)
        {
            Tasks.Add(item);
        }
    }

    private void OnDeleteTaskClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not TaskEntry entry)
        {
            return;
        }

        _repository.Delete(entry.Id);
        _allTasks.Remove(entry);
        Tasks.Remove(entry);
    }
}
