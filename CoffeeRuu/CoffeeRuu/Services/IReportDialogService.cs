namespace CoffeeRuu.Services
{
    /// <summary>
    /// Tách các hộp thoại (MessageBox, SaveFileDialog) ra khỏi ViewModel của báo cáo.
    /// </summary>
    public interface IReportDialogService
    {
        void ShowInfo(string title, string message);
        void ShowWarning(string title, string message);
        void ShowError(string title, string message);

        /// <summary>Mở hộp thoại lưu file Excel. Trả về null nếu người dùng bấm Hủy.</summary>
        string? PickExcelSavePath(string defaultFileName);
    }
}
