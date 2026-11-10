
using System;
using System.Linq;
using CoffeeRuu.Data;
using Microsoft.EntityFrameworkCore;

namespace CoffeeRuu.Services
{
    public class AuthenticatedUser
    {
        public int EmployeeId { get; set; }
        public int RoleId { get; set; }
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string RoleName { get; set; } = "";
    }

    public class AuthenticationService
    {
        private readonly Func<CoffeeRuuDbContext> _contextFactory;

        public AuthenticationService(
            Func<CoffeeRuuDbContext> contextFactory)
        {
            _contextFactory = contextFactory
                ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        public AuthenticatedUser? Authenticate(
            string username,
            string password)
        {
            using var db = _contextFactory();

            var employee = db.Employees
                .FirstOrDefault(e => e.Username == username);

            if (employee == null ||
                !employee.IsActive ||
                string.IsNullOrWhiteSpace(employee.PasswordHash) ||
                !PasswordService.VerifyPassword(
                    password,
                    employee.PasswordHash))
            {
                return null;
            }

            var role = db.Roles.FirstOrDefault(
                r => r.RoleId == employee.RoleId);

            if (role == null)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy vai trò của tài khoản.");
            }

            return new AuthenticatedUser
            {
                EmployeeId = employee.EmployeeId,
                RoleId = employee.RoleId,
                Username = employee.Username,
                FullName = employee.FullName,
                RoleName = role.RoleName
            };
        }
    }
}
