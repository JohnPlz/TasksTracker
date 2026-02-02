using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using TaskTracker.Models;
using TaskTracker.Services;

namespace TaskTracker;

public partial class MainPage : ContentPage
{
	private readonly TaskRepository _repository;

	public ObservableCollection<TaskEntry> Tasks { get; } = new();

	public string Description
	{
		get => _description;
		set
		{
			_description = value;
			OnPropertyChanged();
		}
	}

	public DateTime StartDate
	{
		get => _startDate;
		set
		{
			_startDate = value;
			OnPropertyChanged();
		}
	}

	public TimeSpan StartTime
	{
		get => _startTime;
		set
		{
			_startTime = value;
			OnPropertyChanged();
		}
	}

	public DateTime EndDate
	{
		get => _endDate;
		set
		{
			_endDate = value;
			OnPropertyChanged();
		}
	}

	public TimeSpan EndTime
	{
		get => _endTime;
		set
		{
			_endTime = value;
			OnPropertyChanged();
		}
	}

	private string _description = string.Empty;
	private DateTime _startDate = DateTime.Today;
	private TimeSpan _startTime = DateTime.Now.TimeOfDay;
	private DateTime _endDate = DateTime.Today;
	private TimeSpan _endTime = DateTime.Now.TimeOfDay;

	public MainPage()
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
		Tasks.Clear();
		foreach (var item in _repository.GetAll())
		{
			Tasks.Add(item);
		}
	}

	private async void OnSaveTaskClicked(object? sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(Description))
		{
			await DisplayAlert("Missing description", "Please enter a task description.", "OK");
			return;
		}

		var start = StartDate.Date.Add(StartTime);
		var end = EndDate.Date.Add(EndTime);

		if (end < start)
		{
			await DisplayAlert("Invalid time range", "End time must be after start time.", "OK");
			return;
		}

		var duration = end - start;
		var entry = new TaskEntry
		{
			Description = Description.Trim(),
			StartTime = start,
			EndTime = end,
			DurationMinutes = duration.TotalMinutes
		};

		_repository.Add(entry);
		Tasks.Insert(0, entry);

		Description = string.Empty;
		StartDate = DateTime.Today;
		EndDate = DateTime.Today;
		StartTime = DateTime.Now.TimeOfDay;
		EndTime = DateTime.Now.TimeOfDay;
	}
}
