using System.Collections.ObjectModel;
using TaskTracker.Models;
using TaskTracker.Services;

namespace TaskTracker;

public partial class MetersPage : ContentPage
{
    private readonly MeterRepository _repository;

    public ObservableCollection<Meter> Meters { get; } = new();

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
        Meters.Clear();
        foreach (var meter in _repository.GetAll())
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
