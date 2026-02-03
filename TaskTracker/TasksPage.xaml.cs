using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using ClosedXML.Excel;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;
using Microsoft.Maui.Storage;
using Microsoft.Extensions.DependencyInjection;
using TaskTracker.Models;
using TaskTracker.Services;

namespace TaskTracker;

public partial class TasksPage : ContentPage
{
    private readonly TaskRepository _repository;
    private readonly List<TaskEntry> _allTasks = new();

    public ObservableCollection<TaskEntry> Tasks { get; } = new();
    public ObservableCollection<int> Years { get; } = new();

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

    private string _searchText = string.Empty;
    private bool _useDateFilter;
    private DateTime _filterFrom = DateTime.Today.AddDays(-30);
    private DateTime _filterTo = DateTime.Today;
    private int? _selectedYear;

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
        UpdateYears();
        ApplyFilters();
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

    private async void OnDeleteTaskClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not TaskEntry entry)
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
        Tasks.Remove(entry);
    }

    private async void OnExportExcelClicked(object? sender, EventArgs e)
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
            using var workbook = new XLWorkbook();
            var worksheet = workbook.AddWorksheet("Aufgaben");
            var row = 1;
            worksheet.Cell(row, 1).Value = "Datum";
            worksheet.Cell(row, 2).Value = "Beschreibung";
            worksheet.Cell(row, 3).Value = "Dauer (Stunden)";
            worksheet.Row(row).Style.Font.Bold = true;
            row++;

            var culture = CultureInfo.GetCultureInfo("de-DE");
            foreach (var monthGroup in yearTasks.GroupBy(item => item.StartTime.Month))
            {
                foreach (var entry in monthGroup)
                {
                    worksheet.Cell(row, 1).Value = entry.StartTime.Date;
                    worksheet.Cell(row, 1).Style.DateFormat.Format = "dd.MM.yyyy";
                    worksheet.Cell(row, 2).Value = entry.Description;
                    worksheet.Cell(row, 3).Value = Math.Round(entry.DurationMinutes / 60d, 2);
                    worksheet.Cell(row, 3).Style.NumberFormat.Format = "0.00";
                    row++;
                }

                var monthHours = monthGroup.Sum(item => item.DurationMinutes) / 60d;
                worksheet.Cell(row, 2).Value = $"Summe {culture.DateTimeFormat.GetMonthName(monthGroup.Key)}";
                worksheet.Cell(row, 3).Value = Math.Round(monthHours, 2);
                worksheet.Cell(row, 3).Style.NumberFormat.Format = "0.00";
                worksheet.Row(row).Style.Font.Bold = true;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            await using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var result = await FileSaver.Default.SaveAsync($"tasks-{year}.xlsx", stream, CancellationToken.None);
            if (result.IsSuccessful)
            {
                await Toast.Make("Excel export saved").Show();
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

    private async void OnBackupDatabaseClicked(object? sender, EventArgs e)
    {
        try
        {
            var fileName = $"tasktracker-backup-{DateTime.Now:yyyyMMdd-HHmm}.db";
            await using var stream = new FileStream(_repository.DatabasePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var result = await FileSaver.Default.SaveAsync(fileName, stream, CancellationToken.None);
            if (result.IsSuccessful)
            {
                await Toast.Make("Backup saved").Show();
            }
            else
            {
                await Toast.Make("Backup canceled").Show();
            }
        }
        catch (Exception ex)
        {
            await Toast.Make($"Backup failed: {ex.Message}").Show();
        }
    }

    private async void OnImportBackupClicked(object? sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select backup file",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, new[] { "application/octet-stream", "application/x-sqlite3", "application/db" } },
                    { DevicePlatform.iOS, new[] { "public.data" } },
                    { DevicePlatform.MacCatalyst, new[] { "public.data" } },
                    { DevicePlatform.WinUI, new[] { ".db" } }
                })
            });

            if (result is null)
            {
                await Toast.Make("Import canceled").Show();
                return;
            }

            await using var sourceStream = await result.OpenReadAsync();
            var targetPath = _repository.DatabasePath;
            var tempPath = targetPath + ".tmp";

            await using (var tempStream = File.Create(tempPath))
            {
                await sourceStream.CopyToAsync(tempStream);
            }

            File.Copy(tempPath, targetPath, true);
            File.Delete(tempPath);

            LoadTasks();
            await Toast.Make("Backup imported").Show();
        }
        catch (Exception ex)
        {
            await Toast.Make($"Import failed: {ex.Message}").Show();
        }
    }
}
