using CommunityToolkit.Mvvm.Input;
using RadioFreeDAM07.Models;

namespace RadioFreeDAM07.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}