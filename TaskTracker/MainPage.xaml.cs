using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Media;
using Microsoft.Extensions.DependencyInjection;
using TaskTracker.Models;
using TaskTracker.Services;

namespace TaskTracker;

public partial class MainPage : ContentPage
{
	private readonly TaskRepository _repository;
	private CancellationTokenSource? _recordingCts;

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

	public string RecordButtonText
	{
		get => _recordButtonText;
		set
		{
			_recordButtonText = value;
			OnPropertyChanged();
		}
	}

	public string RecordingStatus
	{
		get => _recordingStatus;
		set
		{
			_recordingStatus = value;
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
			OnPropertyChanged(nameof(EstimatedEndTime));
		}
	}

	public TimeSpan StartTime
	{
		get => _startTime;
		set
		{
			_startTime = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(EstimatedEndTime));
		}
	}

	public double DurationMinutes
	{
		get => _durationMinutes;
		set
		{
			_durationMinutes = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(EstimatedEndTime));
		}
	}

	public DateTime EstimatedEndTime => StartDate.Date.Add(StartTime).AddMinutes(DurationMinutes);

	private string _description = string.Empty;
	private string _recordButtonText = "Record description";
	private string _recordingStatus = string.Empty;
	private DateTime _startDate = DateTime.Today;
	private TimeSpan _startTime = DateTime.Now.TimeOfDay;
	private double _durationMinutes;

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

	private async void OnRecordDescriptionClicked(object? sender, EventArgs e)
	{
		if (_recordingCts is not null)
		{
			_recordingCts.Cancel();
			return;
		}

		var isGranted = await SpeechToText.Default.RequestPermissions();

		if (!isGranted)
		{
			await Toast.Make("Permission required").Show();
			return;
		}

		_recordingCts = new CancellationTokenSource();
		RecordButtonText = "Stop recording";
		RecordingStatus = "Listening...";

		try
		{
			var result = await SpeechToText.Default.ListenAsync(new CultureInfo("de-DE"),
				new Progress<string>(r =>
				{
					RecordingStatus = r;
				}), _recordingCts.Token);
			if (result.Exception is not null)
			{
				await Toast.Make($"Error: {result.Exception.Message}").Show();
				return;
			}

			if (!string.IsNullOrWhiteSpace(result.Text))
			{
				Description = result.Text.Trim();
			}
		}
		catch (OperationCanceledException)
		{
			// Recording stopped by user.
		}
		finally
		{
			_recordingCts?.Dispose();
			_recordingCts = null;
			RecordButtonText = "Record description";
			RecordingStatus = string.Empty;
		}
	}

	private async void OnSaveTaskClicked(object? sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(Description))
		{
			await Toast.Make("Description cannot be empty").Show();
			return;
		}

		if (DurationMinutes <= 0)
		{
			await Toast.Make("Duration must be at least 15 minutes").Show();
			return;
		}

		var start = StartDate.Date.Add(StartTime);
		var duration = TimeSpan.FromMinutes(DurationMinutes);
		var end = start.Add(duration);
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
		StartTime = DateTime.Now.TimeOfDay;
		DurationMinutes = 0;
	}

	private void OnAddDurationClicked(object? sender, EventArgs e)
	{
		if (sender is not Button button)
		{
			return;
		}

		if (button.CommandParameter is null || !double.TryParse(button.CommandParameter.ToString(), out var minutes))
		{
			return;
		}

		DurationMinutes += minutes;
	}

	private void OnDeleteTaskClicked(object? sender, EventArgs e)
	{
		if (sender is not Button button || button.BindingContext is not TaskEntry entry)
		{
			return;
		}

		_repository.Delete(entry.Id);
		Tasks.Remove(entry);
	}
}
