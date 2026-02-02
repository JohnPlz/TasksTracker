using System.Collections.ObjectModel;
using System.Globalization;
using Android.Widget;
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
	private string _recordButtonText = "Record description";
	private string _recordingStatus = string.Empty;
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
			await Toast.Make("Permission required").Show(CancellationToken.None);
			return;
		}

		_recordingCts = new CancellationTokenSource();
		RecordButtonText = "Stop recording";
		RecordingStatus = "Listening...";

		try
		{
			var result = await SpeechToText.Default.ListenAsync(CultureInfo.CurrentCulture,
				new Progress<string>(r =>
				{
					RecordingStatus = r;
				}), _recordingCts.Token);
			if (result.Exception is not null)
			{
				await DisplayAlert("Recording error", result.Exception.Message, "OK");
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
