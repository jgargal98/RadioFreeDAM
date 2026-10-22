using RadioFreeDAM07.Models;
using RadioFreeDAM07.PageModels;

namespace RadioFreeDAM07.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}