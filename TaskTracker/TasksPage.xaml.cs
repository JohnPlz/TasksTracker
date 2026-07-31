using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;
using TaskTracker.Models;
using TaskTracker.Services;

namespace TaskTracker;

public partial class TasksPage : ContentPage
{
    private readonly TaskRepository _repository;
    private readonly List<TaskEntry> _allTasks = new();
    private const int PageSize = 20;

    public ObservableCollection<TaskListItem> TaskItems { get; } = new();
    public ObservableCollection<int> Years { get; } = new();
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

    public int? SelectedYear
    {
        get => _selectedYear;
        set
        {
            _selectedYear = value;
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
    private bool _useDateFilter;
    private DateTime _filterFrom = DateTime.Today.AddDays(-30);
    private DateTime _filterTo = DateTime.Today;
    private int? _selectedYear;
    private bool _isLoading;
    private int _currentPage = 1;
    private int _totalPages = 1;

    public TasksPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        _repository = services?.GetService<TaskRepository>() ?? new TaskRepository();

        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadTasksAsync();
    }

    private async Task LoadTasksAsync()
    {
        try
        {
            IsLoading = true;
            var items = await _repository.GetAllAsync();
            _allTasks.Clear();
            _allTasks.AddRange(items);
            UpdateYears();
            ApplyFilters();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateYears()
    {
        var years = _allTasks
            .Select(item => item.StartTime.Year)
            .Distinct()
            .OrderByDescending(year => year)
            .ToList();

        if (years.Count == 0)
        {
            years.Add(DateTime.Today.Year);
        }

        Years.Clear();
        foreach (var year in years)
        {
            Years.Add(year);
        }

        if (SelectedYear is null || !Years.Contains(SelectedYear.Value))
        {
            SelectedYear = Years.FirstOrDefault();
        }
    }

    private void ApplyFilters(bool resetPage = true)
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

        var filtered = query.ToList();
        UpdatePaging(filtered.Count, resetPage);
        var paged = filtered
            .OrderByDescending(item => item.StartTime)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        BuildTaskItems(paged);
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

    private void BuildTaskItems(IEnumerable<TaskEntry> entries)
    {
        TaskItems.Clear();

        var culture = CultureInfo.GetCultureInfo("de-DE");
        var ordered = entries.OrderByDescending(item => item.StartTime).ToList();
        var monthGroups = ordered.GroupBy(item => new { item.StartTime.Year, item.StartTime.Month });

        foreach (var monthGroup in monthGroups)
        {
            var monthName = culture.DateTimeFormat.GetMonthName(monthGroup.Key.Month);
            TaskItems.Add(new TaskMonthHeaderItem($"{monthName} {monthGroup.Key.Year}"));

            var dateGroups = monthGroup.GroupBy(item => item.StartTime.Date);
            foreach (var dateGroup in dateGroups)
            {
                TaskItems.Add(new TaskDateHeaderItem(dateGroup.Key.ToString("dd.MM.yyyy")));

                foreach (var entry in dateGroup.OrderByDescending(item => item.StartTime))
                {
                    TaskItems.Add(new TaskEntryItem(entry));
                }
            }

            var monthHours = monthGroup.Sum(item => item.DurationMinutes) / 60d;
            TaskItems.Add(new TaskMonthSummaryItem($"Summe {monthName}: {monthHours:F2} Stunden"));
        }
    }

    private async void OnDeleteTaskClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        var entry = button.CommandParameter as TaskEntry
            ?? (button.BindingContext as TaskEntryItem)?.Task;

        if (entry is null)
        {
            return;
        }

        var confirmed = await DisplayAlertAsync("Delete task", "Do you want to delete this task?", "Delete", "Cancel");
        if (!confirmed)
        {
            return;
        }

        _repository.Delete(entry.Id);
        _allTasks.Remove(entry);
        ApplyFilters(false);
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

    private async void OnExportCsvClicked(object? sender, EventArgs e)
    {
        if (SelectedYear is null)
        {
            await Toast.Make("Please select a year").Show();
            return;
        }

        var year = SelectedYear.Value;
        var yearTasks = _allTasks
            .Where(item => item.StartTime.Year == year)
            .OrderBy(item => item.StartTime)
            .ToList();

        if (yearTasks.Count == 0)
        {
            await Toast.Make("No tasks found for selected year").Show();
            return;
        }

        try
        {
            var builder = new StringBuilder();
            builder.AppendLine("ID;Titel;Beschreibung;Standort;Typ;Kategorie;Dauer_Minuten;Status;Datum");

            var exportId = 50;
            foreach (var entry in yearTasks)
            {
                var fields = new[]
                {
                    exportId.ToString(CultureInfo.InvariantCulture),
                    EscapeCsvField(entry.Description),
                    string.Empty,
                    string.Empty,
                    "Wohnung",
                    "Sonstiges",
                    entry.DurationMinutes.ToString("0.##", CultureInfo.InvariantCulture),
                    "ERLEDIGT",
                    entry.StartTime.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)
                };

                builder.AppendLine(string.Join(';', fields));
                exportId++;
            }

            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(builder.ToString()));
            var result = await FileSaver.Default.SaveAsync($"tasks-{year}.csv", stream, CancellationToken.None);
            if (result.IsSuccessful)
            {
                await Toast.Make("CSV export saved").Show();
            }
            else
            {
                await Toast.Make("Export canceled").Show();
            }
        }
        catch (Exception ex)
        {
            await Toast.Make($"Export failed: {ex.Message}").Show();
        }
    }

    private static string EscapeCsvField(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var escaped = value.Replace("\"", "\"\"");
        if (escaped.Contains(';') || escaped.Contains('"') || escaped.Contains('\n') || escaped.Contains('\r'))
        {
            return $"\"{escaped}\"";
        }

        return escaped;
    }

}

public enum TaskListItemKind
{
    Task,
    DateHeader,
    MonthHeader,
    MonthSummary
}

public abstract class TaskListItem
{
    protected TaskListItem(TaskListItemKind kind)
    {
        Kind = kind;
    }

    public TaskListItemKind Kind { get; }
}

public sealed class TaskEntryItem : TaskListItem
{
    public TaskEntryItem(TaskEntry task) : base(TaskListItemKind.Task)
    {
        Task = task;
    }

    public TaskEntry Task { get; }
}

public sealed class TaskDateHeaderItem : TaskListItem
{
    public TaskDateHeaderItem(string dateText) : base(TaskListItemKind.DateHeader)
    {
        DateText = dateText;
    }

    public string DateText { get; }
}

public sealed class TaskMonthHeaderItem : TaskListItem
{
    public TaskMonthHeaderItem(string monthText) : base(TaskListItemKind.MonthHeader)
    {
        MonthText = monthText;
    }

    public string MonthText { get; }
}

public sealed class TaskMonthSummaryItem : TaskListItem
{
    public TaskMonthSummaryItem(string summaryText) : base(TaskListItemKind.MonthSummary)
    {
        SummaryText = summaryText;
    }

    public string SummaryText { get; }
}

public class TaskListItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TaskTemplate { get; set; }
    public DataTemplate? DateHeaderTemplate { get; set; }
    public DataTemplate? MonthHeaderTemplate { get; set; }
    public DataTemplate? MonthSummaryTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        return item switch
        {
            TaskEntryItem => TaskTemplate ?? new DataTemplate(),
            TaskDateHeaderItem => DateHeaderTemplate ?? new DataTemplate(),
            TaskMonthHeaderItem => MonthHeaderTemplate ?? new DataTemplate(),
            TaskMonthSummaryItem => MonthSummaryTemplate ?? new DataTemplate(),
            _ => TaskTemplate ?? new DataTemplate()
        };
    }
}
