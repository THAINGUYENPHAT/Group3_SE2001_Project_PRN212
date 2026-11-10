using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using CoffeeRuu.Models;
using CoffeeRuu.Services;

namespace CoffeeRuu.ViewModels
{
    public class ReportViewModel : BaseViewModel
    {
        private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");

        private readonly ReportService _reportService;
        private readonly ExcelExportService _excelExportService;
        private readonly IReportDialogService _dialogService;

        // Báo cáo của lần lọc gần nhất: dùng cho export để file luôn khớp dữ liệu đang hiển thị.
        private RevenueReport? _currentReport;

        private DateTime? _startDate = DateTime.Today.AddDays(-6);
        private DateTime? _endDate = DateTime.Today;
        private string _revenueText = "0 ₫";
        private string _orderCountText = "0";
        private string _averageOrderText = "0 ₫";
        private string _statusText = string.Empty;
        private bool _isLoading;
        private IReadOnlyList<DailyRevenueRow> _dailyRows = Array.Empty<DailyRevenueRow>();
        private IReadOnlyList<BestSellingProductRow> _productRows = Array.Empty<BestSellingProductRow>();

        public ReportViewModel(
            ReportService reportService,
            ExcelExportService excelExportService,
            IReportDialogService dialogService)
        {
            _reportService = reportService;
            _excelExportService = excelExportService;
            _dialogService = dialogService;

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            ExportCommand = new RelayCommand(Export);
        }

        public DateTime? StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        public DateTime? EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        public string RevenueText
        {
            get => _revenueText;
            private set => SetProperty(ref _revenueText, value);
        }

        public string OrderCountText
        {
            get => _orderCountText;
            private set => SetProperty(ref _orderCountText, value);
        }

        public string AverageOrderText
        {
            get => _averageOrderText;
            private set => SetProperty(ref _averageOrderText, value);
        }

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        public IReadOnlyList<DailyRevenueRow> DailyRows
        {
            get => _dailyRows;
            private set => SetProperty(ref _dailyRows, value);
        }

        public IReadOnlyList<BestSellingProductRow> ProductRows
        {
            get => _productRows;
            private set => SetProperty(ref _productRows, value);
        }

        public ICommand LoadCommand { get; }
        public ICommand ExportCommand { get; }

        public async Task LoadAsync()
        {
            if (IsLoading)
                return;

            if (StartDate is not DateTime startDate || EndDate is not DateTime endDate)
            {
                _dialogService.ShowWarning("Thiếu ngày",
                    "Vui lòng chọn ngày bắt đầu và ngày kết thúc.");
                return;
            }

            if (startDate.Date > endDate.Date)
            {
                _dialogService.ShowWarning("Khoảng thời gian không hợp lệ",
                    "Ngày bắt đầu không được sau ngày kết thúc.");
                return;
            }

            IsLoading = true;
            try
            {
                RevenueReport report = await _reportService.GetReportAsync(startDate, endDate);
                ApplyReport(report);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("Lỗi tải báo cáo",
                    "Không thể tải báo cáo. Hãy kiểm tra kết nối SQL Server và dữ liệu database.\n\n" + ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyReport(RevenueReport report)
        {
            RevenueText = report.TotalRevenue.ToString("N0", ViCulture) + " ₫";
            OrderCountText = report.OrderCount.ToString("N0", ViCulture);
            AverageOrderText = report.AverageOrderValue.ToString("N0", ViCulture) + " ₫";

            DailyRows = report.DailyRows;
            ProductRows = report.ProductRows;
            _currentReport = report;

            StatusText = $"Khoảng thời gian: {report.StartDate:dd/MM/yyyy} - {report.EndDate:dd/MM/yyyy}. " +
                "Chỉ tính đơn Completed có thanh toán Success (không có khoản Refunded). " +
                $"Đã tìm thấy {report.OrderCount} đơn hợp lệ.";
        }

        private void Export()
        {
            if (_currentReport == null)
            {
                _dialogService.ShowInfo("Chưa có dữ liệu",
                    "Hãy bấm \"Lọc báo cáo\" để tải dữ liệu trước khi xuất.");
                return;
            }

            if (_currentReport.DailyRows.Count == 0 && _currentReport.ProductRows.Count == 0)
            {
                _dialogService.ShowInfo("Không có dữ liệu",
                    "Không có dữ liệu trong khoảng thời gian đã chọn để xuất.");
                return;
            }

            string defaultName =
                $"CoffeeRuu_BaoCao_{_currentReport.StartDate:yyyyMMdd}_{_currentReport.EndDate:yyyyMMdd}.xlsx";

            string? path = _dialogService.PickExcelSavePath(defaultName);
            if (path == null)
                return;

            try
            {
                _excelExportService.ExportReport(path, _currentReport);
                _dialogService.ShowInfo("Xuất báo cáo thành công",
                    "Đã xuất báo cáo Excel (.xlsx) thành công.");
            }
            catch (IOException ex)
            {
                _dialogService.ShowError("Lỗi xuất file",
                    "Không thể ghi file. Nếu file đang mở trong Excel, hãy đóng lại rồi thử lại.\n\n" + ex.Message);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("Lỗi xuất file",
                    "Không thể xuất báo cáo.\n\n" + ex.Message);
            }
        }
    }
}
