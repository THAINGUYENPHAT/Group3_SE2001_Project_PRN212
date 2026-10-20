
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading.Tasks;
using CoffeeRuu.Models;
using CoffeeRuu.Repositories;

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
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(
                    "Tên nhà cung cấp không được để trống.");

            if (name.Trim().Length > 150)
                throw new ArgumentException(
                    "Tên nhà cung cấp tối đa 150 ký tự.");

            if (!string.IsNullOrWhiteSpace(phone)
                && phone.Trim().Length > 20)
                throw new ArgumentException(
                    "Số điện thoại tối đa 20 ký tự.");

            if (!string.IsNullOrWhiteSpace(email))
            {
                email = email.Trim();

                if (email.Length > 150)
                    throw new ArgumentException(
                        "Email tối đa 150 ký tự.");

                if (!MailAddress.TryCreate(email, out var mail)
                    || !string.Equals(mail.Address, email,
                        StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException(
                        "Email không đúng định dạng.");
            }

            if (!string.IsNullOrWhiteSpace(address)
                && address.Trim().Length > 300)
                throw new ArgumentException(
                    "Địa chỉ tối đa 300 ký tự.");
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
