using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoffeeRuu.Models;
using CoffeeRuu.Repositories;

namespace CoffeeRuu.Services
{
    public class TableService
    {
        private readonly TableRepository _tableRepository;

        public TableService(TableRepository tableRepository)
        {
            _tableRepository = tableRepository;
        }

        public async Task<List<CafeTable>> GetTablesAsync()
        {
            return await _tableRepository.GetAllAsync();
        }

        public async Task UpdateTableAsync(int id, string name, int capacity)
        {
            ValidateTable(name, capacity);

            bool exists = await _tableRepository.NameExistsAsync(name.Trim(), id);
            if (exists) throw new InvalidOperationException("Tên bàn đã tồn tại ở một bàn khác.");

            bool updated = await _tableRepository.UpdateInfoAsync(id, name.Trim(), capacity);
            if (!updated) throw new KeyNotFoundException("Không tìm thấy bàn.");
        }

        public async Task ChangeTableStatusAsync(int id, string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                throw new ArgumentException("Trạng thái không hợp lệ.");

            bool updated = await _tableRepository.UpdateStatusAsync(id, status);
            if (!updated) throw new KeyNotFoundException("Không tìm thấy bàn.");
        }

        private void ValidateTable(string name, int capacity)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Tên bàn không được để trống.");

            if (name.Trim().Length > 50)
                throw new ArgumentException("Tên bàn tối đa 50 ký tự.");

            if (capacity <= 0)
                throw new ArgumentException("Sức chứa (số ghế) phải lớn hơn 0.");
        }
        public async Task AddTableAsync(string tableName, int capacity, string status)
        {
            ValidateTable(tableName, capacity);

            bool exists = await _tableRepository.NameExistsAsync(tableName.Trim());
            if (exists) throw new InvalidOperationException("Tên bàn đã tồn tại.");

            var newTable = new CafeTable
            {
                TableName = tableName.Trim(),
                Capacity = capacity,
                TableStatus = status,
                IsActive = true
            };

            await _tableRepository.AddAsync(newTable);
        }
        public async Task DeleteTableAsync(int id)
        {
            bool deleted = await _tableRepository.DeleteAsync(id);
            if (!deleted)
            {
                throw new KeyNotFoundException("Không tìm thấy bàn cần xóa.");
            }
        }
    }
}