using Avalonia.Controls;
using Avalonia.Interactivity;
using Dom5Editor.UI.Views;

namespace Dom5Editor.Ava.Views
{
    /// <summary>
    /// "Renumber..." (UI.Views.RenumberViewModel): the open mod's numbers and the start numbers;
    /// Renumber makes the move (one edit of the open mod) and closes the window.
    /// </summary>
    public partial class RenumberWindow : Window
    {
        private readonly RenumberViewModel? _vm;

        public RenumberWindow()
        {
            InitializeComponent();
        }

        public RenumberWindow(RenumberViewModel vm) : this()
        {
            _vm = vm;
            DataContext = vm;
        }

        public RenumberViewModel? ViewModel => _vm;

        private async void Apply_Click(object? sender, RoutedEventArgs e)
        {
            if (_vm == null)
                return;
            var error = _vm.Apply();
            if (error != null)
            {
                await Dialogs.Tell(this, "Nothing was renumbered", error);
                return;
            }
            Close();
        }

        private void Close_Click(object? sender, RoutedEventArgs e) => Close();
    }
}
