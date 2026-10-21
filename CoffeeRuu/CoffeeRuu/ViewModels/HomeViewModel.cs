using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CoffeeRuu.Models;
using CoffeeRuu.Services;

namespace CoffeeRuu.ViewModels
{
    public class HomeViewModel : INotifyPropertyChanged
    {
        private readonly TableService _tableService;

        public ObservableCollection<CafeTable> Tables { get; } = new();

        public HomeViewModel(TableService tableService)
        {
            _tableService = tableService;
        }

        public async Task LoadTablesAsync()
        {
            var items = await _tableService.GetTablesAsync();
            Tables.Clear();
            foreach (var item in items)
            {
                Tables.Add(item);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}