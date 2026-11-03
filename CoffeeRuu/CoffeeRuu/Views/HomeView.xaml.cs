
using System;
using System.Windows.Controls;
using CoffeeRuu.Services;

namespace CoffeeRuu.Views
{
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();

            var session = UserSession.Current;

            if (session == null)
            {
                GreetingText.Text = "Xin chào!";
                RoleTitleText.Text = "Chưa có thông tin vai trò";
                RoleDescriptionText.Text =
                    "Vui lòng đăng nhập để sử dụng các chức năng của CoffeeRuu.";

                return;
            }

            DataContext = session;

            string fullName = string.IsNullOrWhiteSpace(session.FullName)
                ? session.Username
                : session.FullName;

            GreetingText.Text = $"Xin chào, {fullName}!";

            string role = session.RoleName.Trim();

            switch (role.ToLowerInvariant())
            {
                case "system administrator":
                case "admin":
                    RoleTitleText.Text = "Quản trị hệ thống";

                    RoleDescriptionText.Text =
                        "Bạn có thể sử dụng chức năng Employee để quản lý tài khoản nhân viên, " +
                        "cập nhật thông tin và phân quyền truy cập. Chức năng System Logs " +
                        "dùng để theo dõi các hoạt động quan trọng của hệ thống.";
                    break;

                case "cashier":
                    RoleTitleText.Text = "Nhân viên thu ngân";

                    RoleDescriptionText.Text =
                        "Bạn có thể sử dụng Menu để xem thông tin sản phẩm, Orders để xử lý đơn hàng, " +
                        "Table để theo dõi và quản lý bàn, cùng Report để xem các báo cáo được cấp quyền. " +
                        "Hãy kiểm tra thông tin đơn hàng và thanh toán trước khi hoàn tất giao dịch.";
                    break;

                case "barista":
                    RoleTitleText.Text = "Nhân viên pha chế";

                    RoleDescriptionText.Text =
                        "Bạn có thể sử dụng Menu để xem thông tin món uống, kích cỡ và topping; " +
                        "sử dụng KDS (Kitchen Display System) để theo dõi các đơn cần pha chế. " +
                        "Hãy kiểm tra yêu cầu của khách hàng, chuẩn bị món theo thông tin đơn hàng " +
                        "và cập nhật trạng thái pha chế khi chức năng này được cung cấp.";
                    break;

                case "warehouse manager":
                    RoleTitleText.Text = "Quản lý kho";

                    RoleDescriptionText.Text =
                        "Bạn có thể sử dụng Inventory để theo dõi nguyên liệu và tình trạng tồn kho. " +
                        "Các chức năng nhập kho và giao dịch tồn kho hỗ trợ ghi nhận biến động nguyên liệu. " +
                        "Hãy kiểm tra số lượng tồn để kịp thời phát hiện nguyên liệu cần bổ sung.";
                    break;

                default:
                    RoleTitleText.Text = role;

                    RoleDescriptionText.Text =
                        "Các chức năng hiển thị trên hệ thống phụ thuộc vào quyền truy cập " +
                        "được cấp cho tài khoản của bạn.";
                    break;
            }
        }
    }
}
