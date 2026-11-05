
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading.Tasks;
using CoffeeRuu.Models;
using CoffeeRuu.Repositories;
using System.Text.RegularExpressions;
using System.Linq;

namespace CoffeeRuu.Services
{
    public class SupplierService
    {
        private readonly SupplierRepository _supplierRepository;

        public SupplierService(
            SupplierRepository supplierRepository)
        {
            _supplierRepository = supplierRepository;
        }

        // 1. Lấy tất cả nhà cung cấp
        public async Task<List<Supplier>> GetSuppliersAsync()
        {
            return await _supplierRepository.GetAllAsync();
        }

        // 2. Lấy các nhà cung cấp đang hoạt động
        public async Task<List<Supplier>> GetActiveSuppliersAsync()
        {
            return await _supplierRepository.GetActiveAsync();
        }

        // 3. Thêm nhà cung cấp
        public async Task AddSupplierAsync(
            string name,
            string? phone,
            string? email,
            string? address)
        {
            ValidateSupplier(name, phone, email, address);

            var supplier = new Supplier
            {
                SupplierName = name.Trim(),
                Phone = NormalizeOptional(phone),
                Email = NormalizeOptional(email),
                Address = NormalizeOptional(address),
                IsActive = true
            };

            await _supplierRepository.AddAsync(supplier);
        }

        // 4. Cập nhật nhà cung cấp
        public async Task UpdateSupplierAsync(
            int id,
            string name,
            string? phone,
            string? email,
            string? address)
        {
            ValidateSupplier(name, phone, email, address);

            bool updated = await _supplierRepository.UpdateInfoAsync(
                id,
                name.Trim(),
                NormalizeOptional(phone),
                NormalizeOptional(email),
                NormalizeOptional(address)
            );

            if (!updated)
                throw new KeyNotFoundException(
                    "Không tìm thấy nhà cung cấp.");
        }

        // 5. Kích hoạt hoặc ngừng hợp tác
        public async Task SetSupplierActiveAsync(
            int id, bool isActive)
        {
            bool updated = await _supplierRepository
                .SetActiveAsync(id, isActive);

            if (!updated)
                throw new KeyNotFoundException(
                    "Không tìm thấy nhà cung cấp.");
        }

        // 6. Kiểm tra dữ liệu đầu vào

        private void ValidateSupplier(
            string name,
            string? phone,
            string? email,
            string? address)
        {
            // ===================================
            // 1. TÊN NHÀ CUNG CẤP
            // ===================================
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(
                    "Tên nhà cung cấp không được để trống.");

            string cleanName = name.Trim();

            if (cleanName.Length > 150)
                throw new ArgumentException(
                    "Tên nhà cung cấp tối đa 150 ký tự.");

            // Bắt buộc phải có chữ cái
            if (!Regex.IsMatch(cleanName, @"\p{L}"))
                throw new ArgumentException(
                    "Tên nhà cung cấp phải chứa ít nhất một chữ cái.");

            // Cho phép chữ, số và dấu câu thường gặp
            if (!Regex.IsMatch(
                cleanName,
                @"\A[\p{L}\p{M}0-9 .,'’&()/+#-]+\z"))
            {
                throw new ArgumentException(
                    "Tên nhà cung cấp chứa ký tự không hợp lệ.");
            }

            if (!char.IsLetterOrDigit(cleanName[0]))
                throw new ArgumentException(
                    "Tên nhà cung cấp không được bắt đầu bằng ký tự đặc biệt.");


            // Bắt buộc có ít nhất một phương thức liên hệ
            if (string.IsNullOrWhiteSpace(phone) &&
                string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException(
                    "Vui lòng nhập ít nhất số điện thoại hoặc email.");
            }

            // Bắt buộc nhập địa chỉ nhà cung cấp
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException(
                    "Địa chỉ nhà cung cấp không được để trống.");
            }

            // ===================================
            // 2. SỐ ĐIỆN THOẠI
            // ===================================
            if (!string.IsNullOrWhiteSpace(phone))
            {
                string cleanPhone = phone.Trim();

                if (cleanPhone.Length > 20)
                    throw new ArgumentException(
                        "Số điện thoại tối đa 20 ký tự.");

                // Loại bỏ các dấu phân cách thông dụng
                string normalizedPhone = Regex.Replace(
                    cleanPhone, @"[ .()-]", "");

                // Số VN: 10-11 chữ số bắt đầu bằng 0
                // Số quốc tế: + và 8-15 chữ số
                if (!Regex.IsMatch(
                    normalizedPhone,
                    @"\A(?:0[0-9]{9,10}|\+[1-9][0-9]{7,14})\z"))
                {
                    throw new ArgumentException(
                        "Số điện thoại không đúng định dạng.");
                }
            }

            // ===================================
            // 3. EMAIL
            // ===================================
            if (!string.IsNullOrWhiteSpace(email))
            {
                email = email.Trim();

                if (email.Length > 150)
                    throw new ArgumentException(
                        "Email tối đa 150 ký tự.");

                if (!MailAddress.TryCreate(email, out var mail)
                    || !string.Equals(
                        mail.Address,
                        email,
                        StringComparison.OrdinalIgnoreCase)
                    || !mail.Host.Contains('.'))
                {
                    throw new ArgumentException(
                        "Email không đúng định dạng.");
                }
            }

            // ===================================
            // 4. ĐỊA CHỈ
            // ===================================
            if (!string.IsNullOrWhiteSpace(address))
            {
                string cleanAddress = address.Trim();

                if (cleanAddress.Length > 300)
                    throw new ArgumentException(
                        "Địa chỉ tối đa 300 ký tự.");

                // Địa chỉ phải có chữ hoặc số
                if (!Regex.IsMatch(
                    cleanAddress, @"[\p{L}\p{N}]"))
                {
                    throw new ArgumentException(
                        "Địa chỉ không hợp lệ.");
                }

                // Không chấp nhận ký tự điều khiển
                if (cleanAddress.Any(char.IsControl))
                {
                    throw new ArgumentException(
                        "Địa chỉ chứa ký tự không hợp lệ.");
                }
            }
        }


        // 7. Chuẩn hóa thông tin không bắt buộc
        private static string? NormalizeOptional(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim();
        }
    }
}
