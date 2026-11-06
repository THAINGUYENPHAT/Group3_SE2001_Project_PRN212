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

            // Cấu hình lề an toàn 5px để không bị sát mép giấy làm mất chữ
            pd.DefaultPageSettings.Margins = new Margins(5, 5, 5, 5);

            pd.PrintPage += (sender, e) => DrawReceipt(e.Graphics, order);

            try
            {
                pd.Print();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi máy in: {ex.Message}");
            }
        }

        private void DrawReceipt(Graphics g, OrderDto order)
        {
            // Thiết lập đơn vị chuẩn
            g.PageUnit = GraphicsUnit.Pixel;

            // Chiều rộng chuẩn của khổ giấy nhiệt K80 (khoảng 270 - 280px)
            float startX = 5;
            float startY = 10;
            float printWidth = 270; // Độ rộng hiển thị an toàn

            // Font chữ vừa vặn, chuẩn hóa đơn in nhiệt K80
            Font fontHeaderBold = new Font("Arial", 12, FontStyle.Bold);
            Font fontTitleBold = new Font("Arial", 11, FontStyle.Bold);
            Font fontRegular = new Font("Arial", 9, FontStyle.Regular);
            Font fontBold = new Font("Arial", 9, FontStyle.Bold);
            Font fontTotal = new Font("Arial", 10.5f, FontStyle.Bold);
            Font fontSmall = new Font("Arial", 7.5f, FontStyle.Regular);

            Pen dashPen = new Pen(Color.Black, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            Pen solidPen = new Pen(Color.Black, 1);

            StringFormat centerFormat = new StringFormat { Alignment = StringAlignment.Center };
            StringFormat rightFormat = new StringFormat { Alignment = StringAlignment.Far };

            // 1. HEADER
            g.DrawString("COFFEE RUU", fontHeaderBold, Brushes.Black, new RectangleF(startX, startY, printWidth, 20), centerFormat);
            startY += 20;
            g.DrawString("Cần Thơ", fontRegular, Brushes.Black, new RectangleF(startX, startY, printWidth, 16), centerFormat);
            startY += 18;

            g.DrawString("HÓA ĐƠN THANH TOÁN", fontTitleBold, Brushes.Black, new RectangleF(startX, startY, printWidth, 20), centerFormat);
            startY += 20;

            g.DrawString($"Số: {order.OrderCode}", fontBold, Brushes.Black, new RectangleF(startX, startY, printWidth, 18), centerFormat);
            startY += 18;

            g.DrawString($"Ngày: {order.CreatedAt:dd/MM/yyyy HH:mm}", fontRegular, Brushes.Black, startX, startY);
            startY += 16;
            g.DrawString("SL khách: 6", fontRegular, Brushes.Black, startX, startY);
            startY += 16;
            g.DrawString("Thu ngân: Thu Ngân Ca4", fontRegular, Brushes.Black, startX, startY);
            startY += 20;

            // 2. BẢNG MÓN - Cấu hình lại tọa độ cột nằm gọn trong 270px
            g.DrawLine(solidPen, startX, startY, startX + printWidth, startY);

            float col1 = startX;         // STT (#)   -> Rộng 18px
            float col2 = startX + 18;    // Tên món  -> Rộng 102px
            float col3 = startX + 120;   // SL       -> Rộng 25px
            float col4 = startX + 145;   // ĐG       -> Rộng 45px
            float col5 = startX + 190;   // KM       -> Rộng 25px
            float col6 = startX + 215;   // TT       -> Rộng 55px (Vừa đủ mép phải)

            float headerHeight = 20;
            g.DrawString("#", fontBold, Brushes.Black, col1, startY + 3);
            g.DrawString("Tên món", fontBold, Brushes.Black, col2, startY + 3);
            g.DrawString("SL", fontBold, Brushes.Black, col3, startY + 3);
            g.DrawString("ĐG", fontBold, Brushes.Black, col4, startY + 3);
            g.DrawString("KM", fontBold, Brushes.Black, col5, startY + 3);
            g.DrawString("TT", fontBold, Brushes.Black, col6, startY + 3);

            g.DrawLine(solidPen, startX, startY + headerHeight, startX + printWidth, startY + headerHeight);
            startY += headerHeight + 4;

            int index = 1;
            foreach (var item in order.Items)
            {
                float rowHeight = 20;

                g.DrawString(index.ToString(), fontRegular, Brushes.Black, col1, startY + 2);

                // Tên món nếu dài sẽ tự ngắt gọn gàng
                g.DrawString(item.ProductName, fontRegular, Brushes.Black, new RectangleF(col2, startY + 2, 100, rowHeight));

                g.DrawString(item.Quantity.ToString(), fontRegular, Brushes.Black, col3, startY + 2);
                g.DrawString($"{item.Price:N0}", fontRegular, Brushes.Black, col4, startY + 2);
                g.DrawString("", fontRegular, Brushes.Black, col5, startY + 2);
                g.DrawString($"{item.TotalPrice:N0}", fontRegular, Brushes.Black, col6, startY + 2);

                startY += rowHeight;
                index++;
            }

            g.DrawLine(dashPen, startX, startY, startX + printWidth, startY);
            startY += 8;

            // 3. TỔNG CỘNG VÀ THANH TOÁN (Căn lề phải chuẩn xác)
            g.DrawString("Tiền hàng", fontRegular, Brushes.Black, startX, startY);
            g.DrawString($"{order.TotalAmount:N0}đ", fontRegular, Brushes.Black, new RectangleF(startX, startY, printWidth, 18), rightFormat);
            startY += 18;

            g.DrawLine(solidPen, startX, startY, startX + printWidth, startY);
            startY += 6;

            g.DrawString("Tổng thanh toán", fontTotal, Brushes.Black, startX, startY);
            g.DrawString($"{order.TotalAmount:N0}đ", fontTotal, Brushes.Black, new RectangleF(startX, startY, printWidth, 20), rightFormat);
            startY += 20;

            g.DrawString("Tiền mặt", fontRegular, Brushes.Black, startX, startY);
            g.DrawString($"{order.CustomerPaid:N0}đ", fontRegular, Brushes.Black, new RectangleF(startX, startY, printWidth, 18), rightFormat);
            startY += 18;

            g.DrawString("Trả lại khách", fontBold, Brushes.Black, startX, startY);
            g.DrawString($"{order.ChangeAmount:N0}đ", fontBold, Brushes.Black, new RectangleF(startX, startY, printWidth, 18), rightFormat);
            startY += 26;

            // 4. FOOTER
            g.DrawString("Pass Wifi: xincamon", fontBold, Brushes.Black, new RectangleF(startX, startY, printWidth, 18), centerFormat);
            startY += 18;
            g.DrawString("Trân trọng cảm ơn!", fontBold, Brushes.Black, new RectangleF(startX, startY, printWidth, 18), centerFormat);
            startY += 18;
            g.DrawString("CUKCUK - Một sản phẩm của MISA", fontSmall, Brushes.Black, new RectangleF(startX, startY, printWidth, 16), centerFormat);
        }
    }
}