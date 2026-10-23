
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using CoffeeRuu.Models;
using CoffeeRuu.Services;

namespace CoffeeRuu.ViewModels
{
    public class EmployeeManagementViewModel : INotifyPropertyChanged
    {
        private readonly EmployeeService _employeeService;

        public ObservableCollection<Employee> Employees { get; } = new();
        public ObservableCollection<Employee> FilteredEmployees { get; } = new();
        public ObservableCollection<Role> Roles { get; } = new();
        public ObservableCollection<Role> RoleOptions { get; } = new();
        public ObservableCollection<RoleFilterOption> RoleFilters { get; } = new();
        public ObservableCollection<RoleStatistic> RoleStatistics { get; } = new();

        public ObservableCollection<string> StatusFilters { get; } = new()
        {
            "Tất cả trạng thái",
            "Đang hoạt động",
            "Ngừng hoạt động"
        };

        private Employee? _selectedEmployee;

        public Employee? SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                if (_selectedEmployee == value) return;

                _selectedEmployee = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(CanUpdate));
                OnPropertyChanged(nameof(CanDeactivate));
                OnPropertyChanged(nameof(IsSelectedAdmin));
            }
        }

        private string _username = "";
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        private string _fullName = "";
        public string FullName
        {
            get => _fullName;
            set { _fullName = value; OnPropertyChanged(); }
        }

        private string _phone = "";
        public string Phone
        {
            get => _phone;
            set { _phone = value; OnPropertyChanged(); }
        }

        private string _email = "";
        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        private string _password = "";
        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(); }
        }

        private Role? _selectedRole;
        public Role? SelectedRole
        {
            get => _selectedRole;
            set { _selectedRole = value; OnPropertyChanged(); }
        }

        private bool _isFormVisible;

        public bool IsFormVisible
        {
            get => _isFormVisible;
            private set
            {
                _isFormVisible = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormVisibility));
            }
        }

        public Visibility FormVisibility =>
            IsFormVisible ? Visibility.Visible : Visibility.Collapsed;

        private bool _isAddMode;

        public bool IsAddMode
        {
            get => _isAddMode;
            private set
            {
                _isAddMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormTitle));
            }
        }

        public string FormTitle =>
            IsAddMode ? "Thêm nhân viên" : "Chỉnh sửa nhân viên";

        private string _searchText = "";

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        private RoleFilterOption? _selectedRoleFilter;

        public RoleFilterOption? SelectedRoleFilter
        {
            get => _selectedRoleFilter;
            set
            {
                _selectedRoleFilter = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        private string _selectedStatusFilter = "Tất cả trạng thái";

        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                _selectedStatusFilter = value;
                OnPropertyChanged();
                ApplyFilters();
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

        private int _totalCount;

        public int TotalCount
        {
            get => _totalCount;
            private set
            {
                _totalCount = value;
                OnPropertyChanged();
            }
        }

        private int _activeCount;

        public int ActiveCount
        {
            get => _activeCount;
            private set
            {
                _activeCount = value;
                OnPropertyChanged();
            }
        }

        private int _inactiveCount;

        public int InactiveCount
        {
            get => _inactiveCount;
            private set
            {
                _inactiveCount = value;
                OnPropertyChanged();
            }
        }

        public int FilteredCount => FilteredEmployees.Count;

        public bool CanUpdate => SelectedEmployee != null;

        public bool IsSelectedAdmin =>
            IsAdminRole(SelectedEmployee?.Role?.RoleName);

        public bool CanDeactivate =>
            SelectedEmployee != null &&
            SelectedEmployee.IsActive &&
            !IsSelectedAdmin;

        public ICommand LoadCommand { get; }
        public ICommand OpenAddCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditEmployeeCommand { get; }
        public ICommand UpdateCommand { get; }
        public ICommand DeactivateCommand { get; }
        public ICommand ChangePasswordCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ClearCommand { get; }

        public EmployeeManagementViewModel(EmployeeService employeeService)
        {
            _employeeService = employeeService;

            LoadCommand = new RelayCommand(LoadData);
            OpenAddCommand = new RelayCommand(OpenAddForm);
            AddCommand = new RelayCommand(AddEmployee);
            EditEmployeeCommand = new ParameterCommand(EditEmployee);
            UpdateCommand = new RelayCommand(UpdateEmployee);
            DeactivateCommand = new RelayCommand(DeactivateEmployee);
            ChangePasswordCommand = new RelayCommand(ChangePassword);
            CancelCommand = new RelayCommand(CancelForm);
            ClearCommand = new RelayCommand(CancelForm);

            LoadData();
        }

        private static bool IsAdminRole(string? roleName) =>
            string.Equals(
                roleName?.Trim(),
                "Admin",
                StringComparison.OrdinalIgnoreCase);

        private Role? FindRole(int roleId) =>
            Roles.FirstOrDefault(r => r.RoleId == roleId);

        private void OpenAddForm()
        {
            ResetFormFields();
            SelectedEmployee = null;
            IsAddMode = true;
            IsFormVisible = true;
            StatusMessage = "";
        }

        private void EditEmployee(object? parameter)
        {
            if (parameter is not Employee employee) return;

            SelectedEmployee = employee;

            Username = employee.Username;
            FullName = employee.FullName;
            Phone = employee.Phone ?? "";
            Email = employee.Email ?? "";
            IsActive = employee.IsActive;
            SelectedRole = FindRole(employee.RoleId);
            Password = "";

            IsAddMode = false;
            IsFormVisible = true;
            StatusMessage = "";
        }

        private void LoadData()
        {
            try
            {
                var roles = _employeeService.GetRoles();
                var employees = _employeeService.GetAll();

                Roles.Clear();
                RoleOptions.Clear();
                RoleFilters.Clear();

                foreach (var role in roles)
                {
                    Roles.Add(role);

                    if (!IsAdminRole(role.RoleName))
                    {
                        RoleOptions.Add(role);
                    }

                    RoleFilters.Add(
                        new RoleFilterOption(role.RoleId, role.RoleName));
                }

                RoleFilters.Insert(
                    0,
                    new RoleFilterOption(null, "Tất cả vai trò"));

                if (_selectedRoleFilter == null ||
                    !RoleFilters.Any(r => r.RoleId == _selectedRoleFilter.RoleId))
                {
                    SelectedRoleFilter = RoleFilters.FirstOrDefault();
                }

                Employees.Clear();

                foreach (var employee in employees)
                {
                    Employees.Add(employee);
                }

                UpdateStatistics();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                StatusMessage = "Không thể tải dữ liệu: " + ex.Message;

                MessageBox.Show(
                    StatusMessage,
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void UpdateStatistics()
        {
            TotalCount = Employees.Count;
            ActiveCount = Employees.Count(e => e.IsActive);
            InactiveCount = Employees.Count(e => !e.IsActive);

            RoleStatistics.Clear();

            foreach (var role in Roles)
            {
                var roleEmployees = Employees
                    .Where(e => e.RoleId == role.RoleId)
                    .ToList();

                RoleStatistics.Add(new RoleStatistic
                {
                    RoleName = role.RoleName,
                    TotalCount = roleEmployees.Count,
                    ActiveCount = roleEmployees.Count(e => e.IsActive),
                    InactiveCount = roleEmployees.Count(e => !e.IsActive)
                });
            }
        }

        private void ApplyFilters()
        {
            var keyword = (SearchText ?? "").Trim();

            var result = Employees.Where(employee =>
                (
                    string.IsNullOrWhiteSpace(keyword) ||
                    (employee.FullName ?? "").Contains(
                        keyword, StringComparison.OrdinalIgnoreCase) ||
                    (employee.Username ?? "").Contains(
                        keyword, StringComparison.OrdinalIgnoreCase) ||
                    (employee.Phone ?? "").Contains(
                        keyword, StringComparison.OrdinalIgnoreCase)
                ) &&
                (
                    SelectedRoleFilter?.RoleId == null ||
                    employee.RoleId == SelectedRoleFilter.RoleId
                ) &&
                (
                    SelectedStatusFilter == "Tất cả trạng thái" ||
                    (SelectedStatusFilter == "Đang hoạt động" &&
                     employee.IsActive) ||
                    (SelectedStatusFilter == "Ngừng hoạt động" &&
                     !employee.IsActive)
                )).ToList();

            FilteredEmployees.Clear();

            foreach (var employee in result)
            {
                FilteredEmployees.Add(employee);
            }

            if (SelectedEmployee != null &&
                !FilteredEmployees.Any(
                    e => e.EmployeeId == SelectedEmployee.EmployeeId))
            {
                SelectedEmployee = null;

                if (IsFormVisible && !IsAddMode)
                {
                    CancelForm();
                }
            }

            OnPropertyChanged(nameof(FilteredCount));
        }

        private void AddEmployee()
        {
            try
            {
                if (SelectedRole == null)
                {
                    throw new InvalidOperationException(
                        "Vui lòng chọn vai trò.");
                }

                _employeeService.Add(
                    Username,
                    Password,
                    FullName,
                    Phone,
                    Email,
                    SelectedRole.RoleId);

                LoadData();
                CancelForm();

                StatusMessage = "Thêm tài khoản thành công.";

                MessageBox.Show(
                    StatusMessage,
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;

                MessageBox.Show(
                    ex.Message,
                    "Không thể thêm tài khoản",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void UpdateEmployee()
        {
            try
            {
                if (SelectedEmployee == null)
                {
                    throw new InvalidOperationException(
                        "Vui lòng chọn tài khoản cần cập nhật.");
                }

                if (SelectedRole == null)
                {
                    throw new InvalidOperationException(
                        "Vui lòng chọn vai trò.");
                }

                if (IsSelectedAdmin &&
                    SelectedRole.RoleId != SelectedEmployee.RoleId)
                {
                    throw new InvalidOperationException(
                        "Không thể đổi vai trò của tài khoản Admin.");
                }

                if (IsSelectedAdmin && !IsActive)
                {
                    throw new InvalidOperationException(
                        "Không thể vô hiệu hóa tài khoản Admin.");
                }

                _employeeService.Update(
                    SelectedEmployee.EmployeeId,
                    Username,
                    FullName,
                    Phone,
                    Email,
                    SelectedRole.RoleId,
                    IsActive);

                LoadData();
                CancelForm();

                StatusMessage = "Cập nhật tài khoản thành công.";

                MessageBox.Show(
                    StatusMessage,
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;

                MessageBox.Show(
                    ex.Message,
                    "Không thể cập nhật",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void ChangePassword()
        {
            try
            {
                if (SelectedEmployee == null)
                {
                    throw new InvalidOperationException(
                        "Vui lòng chọn tài khoản cần đổi mật khẩu.");
                }

                if (string.IsNullOrWhiteSpace(Password))
                {
                    throw new InvalidOperationException(
                        "Vui lòng nhập mật khẩu mới.");
                }

                _employeeService.ChangePassword(
                    SelectedEmployee.EmployeeId,
                    Password);

                Password = "";
                StatusMessage = "Đổi mật khẩu thành công.";

                MessageBox.Show(
                    StatusMessage,
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;

                MessageBox.Show(
                    ex.Message,
                    "Không thể đổi mật khẩu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void DeactivateEmployee()
        {
            if (SelectedEmployee == null)
            {
                MessageBox.Show(
                    "Vui lòng chọn tài khoản.",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var result = MessageBox.Show(
                $"Bạn có chắc muốn vô hiệu hóa tài khoản '{SelectedEmployee.Username}'?",
                "Xác nhận",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                _employeeService.Deactivate(SelectedEmployee.EmployeeId);

                LoadData();
                CancelForm();

                StatusMessage = "Đã vô hiệu hóa tài khoản.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi: " + ex.Message;

                MessageBox.Show(
                    ex.Message,
                    "Không thể vô hiệu hóa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void ResetFormFields()
        {
            Username = "";
            FullName = "";
            Phone = "";
            Email = "";
            Password = "";
            SelectedRole = null;
            IsActive = true;
        }

        private void CancelForm()
        {
            IsFormVisible = false;
            IsAddMode = false;
            SelectedEmployee = null;
            ResetFormFields();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RoleFilterOption
    {
        public int? RoleId { get; }
        public string RoleName { get; }

        public RoleFilterOption(int? roleId, string roleName)
        {
            RoleId = roleId;
            RoleName = roleName;
        }
    }

    public class RoleStatistic
    {
        public string RoleName { get; set; } = "";
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
    }

    // Command ho tro nhan Employee tu nut Chinh sua trong tung dong DataGrid.
    internal sealed class ParameterCommand : ICommand
    {
        private readonly Action<object?> _execute;

        public ParameterCommand(Action<object?> execute)
        {
            _execute = execute;
        }

        public bool CanExecute(object? parameter) => parameter != null;

        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }
    }
}
