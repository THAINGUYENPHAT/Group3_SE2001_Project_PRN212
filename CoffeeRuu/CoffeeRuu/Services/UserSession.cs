
namespace CoffeeRuu.Services
{
    public class UserSession
    {
        public int EmployeeId { get; private set; }
        public int RoleId { get; private set; }
        public string Username { get; private set; } = string.Empty;
        public string FullName { get; private set; } = string.Empty;
        public string RoleName { get; private set; } = string.Empty;

        public static UserSession? Current { get; private set; }

        public static void Start(
            int employeeId,
            int roleId,
            string username,
            string fullName,
            string roleName)
        {
            Current = new UserSession
            {
                EmployeeId = employeeId,
                RoleId = roleId,
                Username = username,
                FullName = fullName,
                RoleName = roleName
            };
        }

        public static void End()
        {
            Current = null;
        }

        public bool IsAdmin =>
            RoleName.Trim().Equals(
                "System Administrator",
                System.StringComparison.OrdinalIgnoreCase)
            || RoleName.Trim().Equals(
                "Admin",
                System.StringComparison.OrdinalIgnoreCase);

        public bool CanAccess(string screen)
        {
            if (IsAdmin)
                return true;

            string role = RoleName.Trim().ToLowerInvariant();

            switch (screen)
            {
                case "Home":
                case "Menu":
                    return role == "cashier"
                        || role == "barista"
                        || role == "warehouse manager";

                case "Orders":
                    return role == "cashier";

                case "KDS":
                    return role == "barista";

                case "Table":
                    return role == "cashier";

                case "Inventory":
                    return role == "warehouse manager";

                case "Report":
                    return role == "cashier"
                        || role == "warehouse manager";

                case "Product":
                case "Employee":
                    return false;

                default:
                    return false;
            }
        }
    }
}
