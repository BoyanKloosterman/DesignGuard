using CommunityToolkit.Mvvm.ComponentModel;

namespace DesignGuard.ViewModels;

public partial class AppSecurityReviewRowViewModel : ObservableObject
{
    [ObservableProperty] private string _domain = "";

    [ObservableProperty] private string _item = "";

    [ObservableProperty] private string _status = "";

    [ObservableProperty] private string _rationale = "";

    [ObservableProperty] private string _recommendation = "";

    [ObservableProperty] private string _evidence = "";

    [ObservableProperty] private string _sourceTag = "";
}
