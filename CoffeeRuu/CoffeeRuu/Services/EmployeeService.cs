
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using CoffeeRuu.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeRuu.Services
{
    public class EmployeeService
    {
        public List<Employee> GetAll()
        {
            using var db = App.CreateDbContext();

            return db.Employees
                .Include(e => e.Role)
                .OrderBy(e => e.EmployeeId)
                .ToList();
        }

        public List<Role> GetRoles()
        {
            using var db = App.CreateDbContext();

            return db.Roles
                .OrderBy(r => r.RoleName)
                .ToList();
        }

        public void Add(
            string username,
            string password,
            string fullName,
            string? phone,
            string? email,
            int roleId)
        {
            username = (username ?? "").Trim();
            fullName = (fullName ?? "").Trim();

            ValidateUsername(username);
            ValidateFullName(fullName);
            ValidatePassword(password);
            ValidateContact(phone, email);

            using var db = App.CreateDbContext();

            if (db.Employees.Any(e => e.Username == username))
            {
                throw new InvalidOperationException(
                    "Tên đăng nhập đã tồn tại.");
            }

            var role = db.Roles.FirstOrDefault(r => r.RoleId == roleId);

            if (role == null)
            {
                throw new InvalidOperationException(
                    "Vai trò được chọn không tồn tại.");
            }

            if (IsAdminRole(role.RoleName))
            {
                throw new InvalidOperationException(
                    "Không được tạo thêm tài khoản Admin.");
            }

            var employee = new Employee
            {
                Username = username,
                PasswordHash = PasswordService.HashPassword(password),
                FullName = fullName,
                Phone = NormalizePhone(phone),
                Email = NormalizeEmail(email),
                RoleId = roleId,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            db.Employees.Add(employee);
            db.SaveChanges();
        }

        public void Update(
            int employeeId,
            string username,
            string fullName,
            string? phone,
            string? email,
            int roleId,
            bool isActive)
        {
            username = (username ?? "").Trim();
            fullName = (fullName ?? "").Trim();

            ValidateUsername(username);
            ValidateFullName(fullName);
            ValidateContact(phone, email);

            using var db = App.CreateDbContext();

            var employee = db.Employees
                .Include(e => e.Role)
                .FirstOrDefault(e => e.EmployeeId == employeeId);

            if (employee == null)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy nhân viên cần cập nhật.");
            }

            if (db.Employees.Any(e =>
                e.Username == username &&
                e.EmployeeId != employeeId))
            {
                throw new InvalidOperationException(
                    "Tên đăng nhập đã được tài khoản khác sử dụng.");
            }

            var newRole = db.Roles
                .FirstOrDefault(r => r.RoleId == roleId);

            if (newRole == null)
            {
                throw new InvalidOperationException(
                    "Vai trò được chọn không tồn tại.");
            }

            bool currentIsAdmin = IsAdminRole(
                employee.Role?.RoleName);

            bool newRoleIsAdmin = IsAdminRole(newRole.RoleName);

            if (currentIsAdmin && !isActive)
            {
                throw new InvalidOperationException(
                    "Không thể vô hiệu hóa tài khoản Admin.");
            }

            if (currentIsAdmin && employee.RoleId != roleId)
            {
                throw new InvalidOperationException(
                    "Không thể chuyển tài khoản Admin sang vai trò khác.");
            }

            if (!currentIsAdmin && newRoleIsAdmin)
            {
                throw new InvalidOperationException(
                    "Không được chuyển tài khoản nhân viên thành Admin.");
            }

            employee.Username = username;
            employee.FullName = fullName;
            employee.Phone = NormalizePhone(phone);
            employee.Email = NormalizeEmail(email);
            employee.RoleId = roleId;
            employee.IsActive = isActive;
            employee.UpdatedAt = DateTime.Now;

            db.SaveChanges();
        }

        public void ChangePassword(int employeeId, string newPassword)
        {
            ValidatePassword(newPassword);

            using var db = App.CreateDbContext();

            var employee = db.Employees
                .FirstOrDefault(e => e.EmployeeId == employeeId);

            if (employee == null)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy tài khoản cần đổi mật khẩu.");
            }

            employee.PasswordHash =
                PasswordService.HashPassword(newPassword);

            employee.UpdatedAt = DateTime.Now;

            db.SaveChanges();
        }

        public void Deactivate(int employeeId)
        {
            using var db = App.CreateDbContext();

            var employee = db.Employees
                .Include(e => e.Role)
                .FirstOrDefault(e => e.EmployeeId == employeeId);

            if (employee == null)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy tài khoản cần vô hiệu hóa.");
            }

            if (IsAdminRole(employee.Role?.RoleName))
            {
                throw new InvalidOperationException(
                    "Không thể vô hiệu hóa tài khoản Admin.");
            }

            if (!employee.IsActive)
            {
                throw new InvalidOperationException(
                    "Tài khoản này đã được vô hiệu hóa.");
            }

            employee.IsActive = false;
            employee.UpdatedAt = DateTime.Now;

            db.SaveChanges();
        }

        private static void ValidateUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException(
                    "Vui lòng nhập tên đăng nhập.");
            }
        }

        private static void ValidateFullName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new ArgumentException(
                    "Vui lòng nhập họ và tên.");
            }
        }

        private static void ValidatePassword(string? password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException(
                    "Vui lòng nhập mật khẩu.");
            }

            if (password.Length < 6)
            {
                throw new ArgumentException(
                    "Mật khẩu phải có ít nhất 8 ký tự.");
            }
        }

        private static void ValidateContact(
            string? phone,
            string? email)
        {
            if (!string.IsNullOrWhiteSpace(phone))
            {
                string normalizedPhone = phone.Trim();

                string digits = new string(
                    normalizedPhone.Where(char.IsDigit).ToArray());

                if (digits.Length < 9 || digits.Length > 15 ||
                    normalizedPhone.Any(c =>
                        !char.IsDigit(c) &&
                        c != '+' &&
                        c != '-' &&
                        c != ' ' &&
                        c != '(' &&
                        c != ')'))
                {
                    throw new ArgumentException(
                        "Số điện thoại không hợp lệ.");
                }

                if (normalizedPhone.Contains('+') &&
                    normalizedPhone.IndexOf('+') != 0)
                {
                    throw new ArgumentException(
                        "Dấu + trong số điện thoại chỉ được đặt ở đầu.");
                }

                if (normalizedPhone.Count(c => c == '+') > 1)
                {
                    throw new ArgumentException(
                        "Số điện thoại không hợp lệ.");
                }
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                string normalizedEmail = email.Trim();

                try
                {
                    var address = new MailAddress(normalizedEmail);

                    if (!string.Equals(
                        address.Address,
                        normalizedEmail,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        throw new FormatException();
                    }
                }
                catch
                {
                    throw new ArgumentException(
                        "Địa chỉ email không hợp lệ.");
                }
            }
        }

        private static string? NormalizePhone(string? phone)
        {
            return string.IsNullOrWhiteSpace(phone)
                ? null
                : phone.Trim();
        }

        private static string? NormalizeEmail(string? email)
        {
            return string.IsNullOrWhiteSpace(email)
                ? null
                : email.Trim();
        }

        private static bool IsAdminRole(string? roleName)
        {
            return string.Equals(
                roleName?.Trim(),
                "Admin",
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
