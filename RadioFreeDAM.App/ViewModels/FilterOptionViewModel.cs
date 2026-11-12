using CommunityToolkit.Mvvm.ComponentModel;

namespace RadioFreeDAM.ViewModels;

/// <summary>One chip of a filter (a scope or a category). The view paints the selected one differently.</summary>
public partial class FilterOptionViewModel(FilterOption option) : ObservableObject
{
    public string Id { get; } = option.Id;

    public string Name { get; } = option.Name;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
