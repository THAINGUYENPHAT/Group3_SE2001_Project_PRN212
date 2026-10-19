
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using CoffeeRuu.Models;
using CoffeeRuu.Services;

namespace CoffeeRuu.ViewModels
{
    public class SupplierViewModel : INotifyPropertyChanged
    {
        private readonly SupplierService _supplierService;

        // Danh sách nhà cung cấp
        public ObservableCollection<Supplier> Suppliers
        { get; } = new();

        public ICollectionView SuppliersView { get; }

        // Nhà cung cấp được chọn
        private Supplier? _selectedSupplier;

        public Supplier? SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                if (_selectedSupplier == value)
                    return;

                _selectedSupplier = value;
                OnPropertyChanged();

                if (value != null)
                {
                    SupplierNameInput = value.SupplierName;
                    PhoneInput = value.Phone ?? "";
                    EmailInput = value.Email ?? "";
                    AddressInput = value.Address ?? "";
                }
                else
                {
                    ClearInputs();
                }

                OnPropertyChanged(nameof(ToggleButtonText));
                UpdateCommands();
            }
        }

        // Thông tin nhập từ form
        private string _supplierNameInput = "";

        public string SupplierNameInput
        {
            get => _supplierNameInput;
            set
            {
                _supplierNameInput = value;
                OnPropertyChanged();
            }
        }

        private string _phoneInput = "";

        public string PhoneInput
        {
            get => _phoneInput;
            set
            {
                _phoneInput = value;
                OnPropertyChanged();
            }
        }

        private string _emailInput = "";

        public string EmailInput
        {
            get => _emailInput;
            set
            {
                _emailInput = value;
                OnPropertyChanged();
            }
        }

        private string _addressInput = "";

        public string AddressInput
        {
            get => _addressInput;
            set
            {
                _addressInput = value;
                OnPropertyChanged();
            }
        }

        // Tìm kiếm nhà cung cấp
        private string _searchText = "";

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                SuppliersView.Refresh();
            }
        }

        // Trạng thái xử lý
        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                _isBusy = value;
                OnPropertyChanged();
                UpdateCommands();
            }
        }

        private string _statusMessage = "";

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        // Nội dung nút thay đổi trạng thái
        public string ToggleButtonText =>
            SelectedSupplier?.IsActive == true
                ? "Ngừng hợp tác"
                : "Kích hoạt lại";

        // Commands
        public RelayCommand LoadCommand { get; }
        public RelayCommand AddCommand { get; }
        public RelayCommand UpdateCommand { get; }
        public RelayCommand ToggleActiveCommand { get; }
        public RelayCommand ClearCommand { get; }

        // Constructor
        public SupplierViewModel(
            SupplierService supplierService)
        {
            _supplierService = supplierService;

            SuppliersView =
                CollectionViewSource.GetDefaultView(Suppliers);

            SuppliersView.Filter = FilterSupplier;

            LoadCommand = new RelayCommand(
                async () => await ExecuteSafeAsync(
                    LoadSuppliersAsync),
                () => !IsBusy);

            AddCommand = new RelayCommand(
                async () => await ExecuteSafeAsync(
                    AddSupplierAsync),
                () => !IsBusy && SelectedSupplier == null);

            UpdateCommand = new RelayCommand(
                async () => await ExecuteSafeAsync(
                    UpdateSupplierAsync),
                () => !IsBusy && SelectedSupplier != null);

            ToggleActiveCommand = new RelayCommand(
                async () => await ExecuteSafeAsync(
                    ToggleActiveAsync),
                () => !IsBusy && SelectedSupplier != null);

            ClearCommand = new RelayCommand(
                ClearForm,
                () => !IsBusy);

            // Tải dữ liệu khi tạo ViewModel
            _ = ExecuteSafeAsync(LoadSuppliersAsync);
        }

        // 1. Lấy danh sách nhà cung cấp
        private async Task LoadSuppliersAsync()
        {
            int? selectedId = SelectedSupplier?.SupplierId;

            var items = await _supplierService
                .GetSuppliersAsync();

            Suppliers.Clear();

            foreach (var item in items)
            {
                Suppliers.Add(item);
            }

            SelectedSupplier = selectedId.HasValue
                ? Suppliers.FirstOrDefault(
                    s => s.SupplierId == selectedId.Value)
                : null;

            SuppliersView.Refresh();
        }

        // 2. Thêm nhà cung cấp
        private async Task AddSupplierAsync()
        {
            await _supplierService.AddSupplierAsync(
                SupplierNameInput,
                PhoneInput,
                EmailInput,
                AddressInput);

            await LoadSuppliersAsync();

            ClearForm();

            StatusMessage = "Thêm nhà cung cấp thành công.";
        }

        // 3. Cập nhật nhà cung cấp
        private async Task UpdateSupplierAsync()
        {
            if (SelectedSupplier == null)
                return;

            int id = SelectedSupplier.SupplierId;

            await _supplierService.UpdateSupplierAsync(
                id,
                SupplierNameInput,
                PhoneInput,
                EmailInput,
                AddressInput);

            await LoadSuppliersAsync();

            StatusMessage = "Cập nhật nhà cung cấp thành công.";
        }

        // 4. Kích hoạt / ngừng hợp tác
        private async Task ToggleActiveAsync()
        {
            if (SelectedSupplier == null)
                return;

            bool newStatus = !SelectedSupplier.IsActive;

            await _supplierService.SetSupplierActiveAsync(
                SelectedSupplier.SupplierId,
                newStatus);

            await LoadSuppliersAsync();

            StatusMessage = newStatus
                ? "Đã kích hoạt nhà cung cấp."
                : "Đã ngừng hợp tác với nhà cung cấp.";
        }

        // 5. Lọc danh sách nhà cung cấp
        private bool FilterSupplier(object obj)
        {
            if (obj is not Supplier supplier)
                return false;

            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            string keyword = SearchText.Trim();

            return supplier.SupplierName.Contains(
                       keyword,
                       StringComparison.OrdinalIgnoreCase)
                   || (supplier.Phone?.Contains(
                       keyword,
                       StringComparison.OrdinalIgnoreCase) ?? false)
                   || (supplier.Email?.Contains(
                       keyword,
                       StringComparison.OrdinalIgnoreCase) ?? false);
        }

        // 6. Xóa thông tin nhập
        private void ClearInputs()
        {
            SupplierNameInput = "";
            PhoneInput = "";
            EmailInput = "";
            AddressInput = "";
        }

        private void ClearForm()
        {
            SelectedSupplier = null;
            ClearInputs();
        }

        // 7. Xử lý thao tác bất đồng bộ
        private async Task ExecuteSafeAsync(Func<Task> action)
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = "";

                await action();
            }
            catch (ArgumentException ex)
            {
                StatusMessage = ex.Message;
            }
            catch (InvalidOperationException ex)
            {
                StatusMessage = ex.Message;
            }
            catch (KeyNotFoundException ex)
            {
                StatusMessage = ex.Message;
            }
            catch (Exception ex)
            {
                StatusMessage =
                    "Có lỗi xảy ra: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        // 8. Cập nhật trạng thái các Command
        private void UpdateCommands()
        {
            LoadCommand?.RaiseCanExecuteChanged();
            AddCommand?.RaiseCanExecuteChanged();
            UpdateCommand?.RaiseCanExecuteChanged();
            ToggleActiveCommand?.RaiseCanExecuteChanged();
            ClearCommand?.RaiseCanExecuteChanged();
        }

        // 9. Thông báo thay đổi thuộc tính
        public event PropertyChangedEventHandler?
            PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
