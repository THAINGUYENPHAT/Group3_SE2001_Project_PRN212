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
            // Cấu hình khổ giấy in bill nhiệt 80mm (hoặc 58mm)
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
            // Các Font chữ chuẩn trên hóa đơn
            Font fontHeaderBold = new Font("Arial", 12, FontStyle.Bold);
            Font fontTitleBold = new Font("Arial", 11, FontStyle.Bold);
            Font fontRegular = new Font("Arial", 8.5f, FontStyle.Regular);
            Font fontBold = new Font("Arial", 8.5f, FontStyle.Bold);
            Font fontTotal = new Font("Arial", 10f, FontStyle.Bold);

            // Bút vẽ đường kẻ nét đứt & nét liền
            Pen dashPen = new Pen(Color.Black, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            Pen solidPen = new Pen(Color.Black, 1);

            float startX = 5;
            float startY = 10;
            float width = 270; // Độ rộng tiêu chuẩn bill K80

            StringFormat centerFormat = new StringFormat { Alignment = StringAlignment.Center };
            StringFormat rightFormat = new StringFormat { Alignment = StringAlignment.Far };

            // 1. HEADER TÊN QUÁN & ĐỊA CHỈ
            g.DrawString("COFFEE RUU", fontHeaderBold, Brushes.Black, new RectangleF(startX, startY, width, 20), centerFormat);
            startY += 18;
            g.DrawString("Cần Thơ", fontRegular, Brushes.Black, new RectangleF(startX, startY, width, 18), centerFormat);
            startY += 18;

            // Tiêu đề Hóa đơn
            g.DrawString("HÓA ĐƠN THANH TOÁN", fontTitleBold, Brushes.Black, new RectangleF(startX, startY, width, 20), centerFormat);
            startY += 20;

            // Số hóa đơn & Ngày giờ
            g.DrawString($"Số: {order.OrderCode}", fontBold, Brushes.Black, new RectangleF(startX, startY, width, 18), centerFormat);
            startY += 18;
            g.DrawString($"Ngày: {order.CreatedAt:dd/MM/yyyy (HH:mm SA - HH:mm SA)}", fontRegular, Brushes.Black, startX, startY);
            startY += 16;

            // Bàn / SL khách / Thu ngân
            //g.DrawString($"Bàn: {(string.IsNullOrEmpty(order.TableName) ? "01" : order.TableName)}", fontRegular, Brushes.Black, startX, startY);
            //startY += 16;
            g.DrawString("SL khách: 6", fontRegular, Brushes.Black, startX, startY);
            startY += 16;
            g.DrawString("Thu ngân: Thu Ngân Ca4", fontRegular, Brushes.Black, startX, startY);
            startY += 20;

            // 2. BẢNG CHI TIẾT MÓN (Có khung ô vuông)
            g.DrawLine(solidPen, startX, startY, startX + width, startY);

            // Tiêu đề cột
            float col1 = startX;       // #
            float col2 = startX + 25;  // Tên món
            float col3 = startX + 130; // SL
            float col4 = startX + 160; // ĐG
            float col5 = startX + 215; // % KM
            float col6 = startX + 235; // TT

            float headerHeight = 22;
            g.DrawString("#", fontBold, Brushes.Black, col1 + 5, startY + 4);
            g.DrawString("Tên món", fontBold, Brushes.Black, col2 + 5, startY + 4);
            g.DrawString("SL", fontBold, Brushes.Black, col3 + 2, startY + 4);
            g.DrawString("ĐG", fontBold, Brushes.Black, col4 + 5, startY + 4);
            g.DrawString("%\nKM", new Font("Arial", 7, FontStyle.Bold), Brushes.Black, col5, startY + 1);
            g.DrawString("TT", fontBold, Brushes.Black, col6 + 5, startY + 4);

            // Kẻ khung ngang dưới tiêu đề
            g.DrawLine(solidPen, startX, startY + headerHeight, startX + width, startY + headerHeight);
            startY += headerHeight;

            int index = 1;
            foreach (var item in order.Items)
            {
                float rowHeight = 24;
                g.DrawString(index.ToString(), fontRegular, Brushes.Black, col1 + 5, startY + 4);
                g.DrawString(item.ProductName, fontRegular, Brushes.Black, col2 + 2, startY + 4);
                g.DrawString(item.Quantity.ToString(), fontRegular, Brushes.Black, col3 + 5, startY + 4);
                g.DrawString($"{item.Price:N0}", fontRegular, Brushes.Black, col4 + 2, startY + 4);
                g.DrawString("", fontRegular, Brushes.Black, col5, startY + 4);
                g.DrawString($"{item.TotalPrice:N0}", fontRegular, Brushes.Black, col6, startY + 4);

                startY += rowHeight;
                index++;
            }

            // Kẻ nét đứt phân cách bảng
            g.DrawLine(dashPen, startX, startY, startX + width, startY);
            startY += 8;

            // 3. TỔNG CỘNG VÀ THANH TOÁN
            g.DrawString("Tiền hàng", fontRegular, Brushes.Black, startX, startY);
            g.DrawString($"{order.TotalAmount:N0}đ", fontRegular, Brushes.Black, new RectangleF(startX, startY, width, 18), rightFormat);
            startY += 20;

            // Đường gạch ngang nét liền dưới Tiền hàng
            g.DrawLine(solidPen, startX, startY, startX + width, startY);
            startY += 6;

            g.DrawString("Tổng thanh toán", fontTotal, Brushes.Black, startX, startY);
            g.DrawString($"{order.TotalAmount:N0}đ", fontTotal, Brushes.Black, new RectangleF(startX, startY, width, 22), rightFormat);
            startY += 22;

            g.DrawString("Tiền mặt", fontRegular, Brushes.Black, startX, startY);
            g.DrawString($"{order.CustomerPaid:N0}đ", fontRegular, Brushes.Black, new RectangleF(startX, startY, width, 18), rightFormat);
            startY += 18;

            g.DrawString("Trả lại khách", fontBold, Brushes.Black, startX, startY);
            g.DrawString($"{order.ChangeAmount:N0}đ", fontBold, Brushes.Black, new RectangleF(startX, startY, width, 18), rightFormat);
            startY += 30;

            // 4. FOOTER THÔNG TIN WIFI & LỜI CẢM ƠN
            g.DrawString("Pass Wifi: xincamon", fontBold, Brushes.Black, new RectangleF(startX, startY, width, 18), centerFormat);
            startY += 18;
            g.DrawString("Trân trọng cảm ơn!", fontBold, Brushes.Black, new RectangleF(startX, startY, width, 18), centerFormat);
            startY += 18;
            g.DrawString("CUKCUK - Một sản phẩm của MISA", new Font("Arial", 7.5f, FontStyle.Regular), Brushes.Black, new RectangleF(startX, startY, width, 16), centerFormat);
        }
    }
}