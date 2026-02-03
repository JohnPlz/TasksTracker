namespace TaskTracker;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute("meterdetail", typeof(MeterDetailPage));
	}
}
