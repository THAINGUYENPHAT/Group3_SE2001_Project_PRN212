using System;
using System.Drawing;
using System.Drawing.Printing;
using CoffeeRuu.Models;

namespace CoffeeRuu.Services
{
    public interface IPrintService
    {
        void PrintReceipt(OrderDto order);
    }

    public class PrintService : IPrintService
    {
        public void PrintReceipt(OrderDto order)
        {
            PrintDocument pd = new PrintDocument();

            // Thiết lập lề 40 point (~1.4 cm) để cân đối trên tờ A4
            pd.DefaultPageSettings.Margins = new Margins(40, 40, 40, 40);
            pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 595, 842);

            pd.PrintPage += (sender, e) => DrawReceiptA4(e.Graphics, order);

            try
            {
                pd.Print();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi máy in: {ex.Message}");
            }
        }

        private void DrawReceiptA4(Graphics g, OrderDto order)
        {
            // Thiết lập đơn vị Point chuẩn cho A4
            g.PageUnit = GraphicsUnit.Point;

            float startX = 40;
            float startY = 40;
            float printWidth = 515; // 595 - (40*2) = 515 points (Độ rộng vùng in A4)

            // Font chữ chuẩn kích thước A4 (To, rõ, chuẩn báo cáo/hóa đơn)
            Font fontHeaderBold = new Font("Arial", 18, FontStyle.Bold);
            Font fontTitleBold = new Font("Arial", 15, FontStyle.Bold);
            Font fontRegular = new Font("Arial", 11, FontStyle.Regular);
            Font fontBold = new Font("Arial", 11, FontStyle.Bold);
            Font fontTotal = new Font("Arial", 13, FontStyle.Bold);
            Font fontSmall = new Font("Arial", 9.5f, FontStyle.Regular);

            Pen dashPen = new Pen(Color.Black, 1f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            Pen solidPen = new Pen(Color.Black, 1.5f);

            StringFormat centerFormat = new StringFormat { Alignment = StringAlignment.Center };
            StringFormat rightFormat = new StringFormat { Alignment = StringAlignment.Far };

            // 1. HEADER TÊN QUÁN & THÔNG TIN HÓA ĐƠN
            g.DrawString("COFFEE RUU", fontHeaderBold, Brushes.Black, new RectangleF(startX, startY, printWidth, 26), centerFormat);
            startY += 26;
            g.DrawString("Cần Thơ", fontRegular, Brushes.Black, new RectangleF(startX, startY, printWidth, 20), centerFormat);
            startY += 24;

            g.DrawString("HÓA ĐƠN THANH TOÁN", fontTitleBold, Brushes.Black, new RectangleF(startX, startY, printWidth, 24), centerFormat);
            startY += 28;

            g.DrawString($"Số: {order.OrderCode}", fontBold, Brushes.Black, new RectangleF(startX, startY, printWidth, 20), centerFormat);
            startY += 26;

            // Thông tin chung (Hiển thị 2 bên cho thoáng A4)
            g.DrawString($"Ngày: {order.CreatedAt:dd/MM/yyyy HH:mm}", fontRegular, Brushes.Black, startX, startY);
            g.DrawString("SL khách: 6", fontRegular, Brushes.Black, new RectangleF(startX, startY, printWidth, 20), rightFormat);
            startY += 22;
            g.DrawString("Thu ngân: Thu Ngân Ca4", fontRegular, Brushes.Black, startX, startY);
            startY += 26;

            // 2. BẢNG MÓN (Tọa độ cột trải dài full chiều rộng A4)
            g.DrawLine(solidPen, startX, startY, startX + printWidth, startY);

            float col1 = startX;         // STT (#)   -> Rộng 30px
            float col2 = startX + 30;    // Tên món  -> Rộng 230px
            float col3 = startX + 260;   // SL       -> Rộng 50px
            float col4 = startX + 310;   // ĐG       -> Rộng 70px
            float col5 = startX + 380;   // % KM     -> Rộng 55px
            float col6 = startX + 435;   // TT       -> Rộng 80px (Vừa sát lề phải)

            float headerHeight = 24;
            g.DrawString("#", fontBold, Brushes.Black, col1, startY + 4);
            g.DrawString("Tên món", fontBold, Brushes.Black, col2, startY + 4);
            g.DrawString("SL", fontBold, Brushes.Black, col3, startY + 4);
            g.DrawString("ĐG", fontBold, Brushes.Black, col4, startY + 4);
            g.DrawString("% KM", fontBold, Brushes.Black, col5, startY + 4);
            g.DrawString("TT", fontBold, Brushes.Black, col6, startY + 4);

            g.DrawLine(solidPen, startX, startY + headerHeight, startX + printWidth, startY + headerHeight);
            startY += headerHeight + 6;

            int index = 1;
            foreach (var item in order.Items)
            {
                float rowHeight = 24;

                g.DrawString(index.ToString(), fontRegular, Brushes.Black, col1, startY + 3);
                g.DrawString(item.ProductName, fontRegular, Brushes.Black, new RectangleF(col2, startY + 3, 225, rowHeight));
                g.DrawString(item.Quantity.ToString(), fontRegular, Brushes.Black, col3, startY + 3);
                g.DrawString($"{item.Price:N0}", fontRegular, Brushes.Black, col4, startY + 3);
                g.DrawString("", fontRegular, Brushes.Black, col5, startY + 3);
                g.DrawString($"{item.TotalPrice:N0}", fontRegular, Brushes.Black, col6, startY + 3);

                startY += rowHeight;
                index++;
            }

            g.DrawLine(dashPen, startX, startY, startX + printWidth, startY);
            startY += 12;

            // 3. TỔNG CỘNG VÀ THANH TOÁN
            g.DrawString("Tiền hàng", fontRegular, Brushes.Black, startX, startY);
            g.DrawString($"{order.TotalAmount:N0}đ", fontRegular, Brushes.Black, new RectangleF(startX, startY, printWidth, 20), rightFormat);
            startY += 24;

            g.DrawLine(solidPen, startX, startY, startX + printWidth, startY);
            startY += 8;

            g.DrawString("Tổng thanh toán", fontTotal, Brushes.Black, startX, startY);
            g.DrawString($"{order.TotalAmount:N0}đ", fontTotal, Brushes.Black, new RectangleF(startX, startY, printWidth, 24), rightFormat);
            startY += 26;

            g.DrawString("Tiền mặt", fontRegular, Brushes.Black, startX, startY);
            g.DrawString($"{order.CustomerPaid:N0}đ", fontRegular, Brushes.Black, new RectangleF(startX, startY, printWidth, 20), rightFormat);
            startY += 22;

            g.DrawString("Trả lại khách", fontBold, Brushes.Black, startX, startY);
            g.DrawString($"{order.ChangeAmount:N0}đ", fontBold, Brushes.Black, new RectangleF(startX, startY, printWidth, 20), rightFormat);
            startY += 40;

            // 4. FOOTER
            g.DrawString("Pass Wifi: xincamon", fontBold, Brushes.Black, new RectangleF(startX, startY, printWidth, 20), centerFormat);
            startY += 22;
            g.DrawString("Trân trọng cảm ơn!", fontBold, Brushes.Black, new RectangleF(startX, startY, printWidth, 20), centerFormat);
            startY += 22;
            g.DrawString("CUKCUK - Một sản phẩm của MISA", fontSmall, Brushes.Black, new RectangleF(startX, startY, printWidth, 18), centerFormat);
        }
    }
}