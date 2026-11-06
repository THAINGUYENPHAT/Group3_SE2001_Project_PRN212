using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;
using CoffeeRuu.Models;

namespace CoffeeRuu.Services
{
    /// <summary>
    /// Xuất báo cáo ra file .xlsx tối giản bằng Open XML (không cần NuGet thêm).
    /// </summary>
    public class ExcelExportService
    {
        public void ExportReport(string filePath, RevenueReport report)
        {
            decimal totalRevenue = report.DailyRows.Sum(r => r.Revenue);
            int totalOrders = report.DailyRows.Sum(r => r.OrderCount);

            var rows = new List<XlsxRow>
            {
                new(new object?[] { "COFFEERUU - BÁO CÁO DOANH THU" }, XlsxStyle.Bold),
                new(new object?[] { "Từ ngày", report.StartDate.ToString("dd/MM/yyyy") }),
                new(new object?[] { "Đến ngày", report.EndDate.ToString("dd/MM/yyyy") }),
                new(new object?[] { "Quy tắc", "Chỉ tính đơn Completed có thanh toán Success (không có khoản Refunded)" }),
                new(new object?[] { "Tổng doanh thu (VND)", totalRevenue }),
                new(new object?[] { "Tổng số đơn", totalOrders }),
                new(new object?[] { }),
                new(new object?[] { "DOANH THU THEO NGÀY" }, XlsxStyle.Bold),
                new(new object?[] { "Ngày", "Số đơn", "Doanh thu (VND)" }, XlsxStyle.Bold)
            };

            foreach (var row in report.DailyRows)
                rows.Add(new XlsxRow(new object?[] { row.DateLabel, row.OrderCount, row.Revenue }));

            rows.Add(new XlsxRow(new object?[] { }));
            rows.Add(new XlsxRow(new object?[] { "SẢN PHẨM BÁN CHẠY (xếp theo số lượng bán)" }, XlsxStyle.Bold));
            rows.Add(new XlsxRow(new object?[] { "Hạng", "Sản phẩm", "Số lượng bán", "Doanh thu (VND)" }, XlsxStyle.Bold));

            foreach (var row in report.ProductRows)
                rows.Add(new XlsxRow(new object?[] { row.Rank, row.ProductName, row.QuantitySold, row.Revenue }));

            WriteSimpleXlsx(filePath, rows);
        }

        // ---------- Ghi file .xlsx ----------

        private enum XlsxStyle { Normal = 0, Bold = 1 }

        private sealed record XlsxRow(object?[] Cells, XlsxStyle Style = XlsxStyle.Normal);

        private static void WriteSimpleXlsx(string filePath, IReadOnlyList<XlsxRow> rows)
        {
            using var file = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite);
            using var archive = new ZipArchive(file, ZipArchiveMode.Create);

            WriteZipText(archive, "[Content_Types].xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
                "</Types>");

            WriteZipText(archive, "_rels/.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                "</Relationships>");

            WriteZipText(archive, "xl/workbook.xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
                "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                "<sheets><sheet name=\"BaoCao\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");

            WriteZipText(archive, "xl/_rels/workbook.xml.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
                "</Relationships>");

            // cellXfs: 0 = mặc định, 1 = chữ đậm, 2 = số định dạng #,##0 (numFmtId 3)
            WriteZipText(archive, "xl/styles.xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                "<fonts count=\"2\">" +
                "<font><sz val=\"11\"/><name val=\"Calibri\"/></font>" +
                "<font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font>" +
                "</fonts>" +
                "<fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills>" +
                "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
                "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
                "<cellXfs count=\"3\">" +
                "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
                "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/>" +
                "<xf numFmtId=\"3\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/>" +
                "</cellXfs>" +
                "</styleSheet>");

            var sheet = new StringBuilder();
            sheet.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sheet.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            sheet.Append("<cols>" +
                         "<col min=\"1\" max=\"1\" width=\"30\" customWidth=\"1\"/>" +
                         "<col min=\"2\" max=\"2\" width=\"34\" customWidth=\"1\"/>" +
                         "<col min=\"3\" max=\"4\" width=\"20\" customWidth=\"1\"/>" +
                         "</cols>");
            sheet.Append("<sheetData>");

            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                int excelRow = rowIndex + 1;
                XlsxRow xlsxRow = rows[rowIndex];
                sheet.Append($"<row r=\"{excelRow}\">");

                for (int col = 0; col < xlsxRow.Cells.Length; col++)
                {
                    object? value = xlsxRow.Cells[col];
                    if (value == null)
                        continue;

                    string cellRef = GetExcelColumnName(col + 1) + excelRow;
                    int boldStyle = xlsxRow.Style == XlsxStyle.Bold ? 1 : 0;

                    if (value is string text)
                    {
                        string escaped = SecurityElement.Escape(text) ?? string.Empty;
                        sheet.Append($"<c r=\"{cellRef}\" t=\"inlineStr\" s=\"{boldStyle}\"><is><t xml:space=\"preserve\">{escaped}</t></is></c>");
                    }
                    else if (value is decimal money)
                    {
                        sheet.Append($"<c r=\"{cellRef}\" s=\"2\"><v>{money.ToString(CultureInfo.InvariantCulture)}</v></c>");
                    }
                    else if (value is IFormattable number)
                    {
                        sheet.Append($"<c r=\"{cellRef}\"><v>{number.ToString(null, CultureInfo.InvariantCulture)}</v></c>");
                    }
                    else
                    {
                        string escaped = SecurityElement.Escape(value.ToString() ?? string.Empty) ?? string.Empty;
                        sheet.Append($"<c r=\"{cellRef}\" t=\"inlineStr\"><is><t>{escaped}</t></is></c>");
                    }
                }

                sheet.Append("</row>");
            }

            sheet.Append("</sheetData></worksheet>");
            WriteZipText(archive, "xl/worksheets/sheet1.xml", sheet.ToString());
        }

        private static void WriteZipText(ZipArchive archive, string entryName, string content)
        {
            var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using var stream = entry.Open();
            using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            writer.Write(content);
        }

        private static string GetExcelColumnName(int columnNumber)
        {
            string name = string.Empty;
            while (columnNumber > 0)
            {
                int remainder = (columnNumber - 1) % 26;
                name = (char)('A' + remainder) + name;
                columnNumber = (columnNumber - 1) / 26;
            }
            return name;
        }
    }
}
