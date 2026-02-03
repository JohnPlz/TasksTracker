using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using TaskTracker.Models;
using TaskTracker.Services;

namespace TaskTracker;

public partial class TasksPage : ContentPage
{
    private readonly TaskRepository _repository;

    public ObservableCollection<TaskEntry> Tasks { get; } = new();

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
        Tasks.Clear();
        foreach (var item in _repository.GetAll())
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
        Tasks.Remove(entry);
    }
}
