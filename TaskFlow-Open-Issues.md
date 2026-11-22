# TaskFlow: vấn đề giải quyết sau khi sản phẩm chạy ổn

Cập nhật: 16/11/2026. Nguyên tắc: làm xong sản phẩm (FR P1) trước, các mục dưới đây xử lý sau.
Các quyết định đã chốt đã được đưa vào `TaskFlow-SRS-Business-Rules-v1.1.docx`; file này chỉ giữ phần còn mở.

## A. Hạ tầng và triển khai (đã chốt: Render, Supabase, Vercel; chi tiết tính sau)

| # | Vấn đề | Gợi ý |
|---|---|---|
| A1 | Render miễn phí ngủ sau 15 phút, khởi động nguội khoảng 1 phút, cắt kết nối SignalR | Chấp nhận ở bản demo. Dùng `/health` để đánh thức, hoặc dịch vụ ping định kỳ khi cần demo |
| A2 | Các dịch vụ miễn phí của Render dùng chung 750 giờ mỗi tháng | Chỉ bật Staging khi cần kiểm thử |
| A3 | WebSocket của SignalR trên gói miễn phí chưa kiểm chứng | Deploy SignalR Hub tối thiểu ngay ngày đầu. Dự phòng: SignalR tự hạ cấp sang SSE hoặc long polling |
| A4 | Web (Vercel) và API (Render) khác site nên cookie `SameSite=Strict` không được gửi | Tạm thời dùng `SameSite=None; Secure` kèm CORS và chống CSRF (đã ghi trong NFR-FE-08). Khi có tên miền riêng, chuyển sang subdomain cùng site |
| A5 | Kết nối Supabase từ Render: chọn chuỗi kết nối trực tiếp hay pooler, Npgsql với pooler. Dự án Supabase miễn phí có thể bị tạm dừng khi lâu không dùng (cần xác minh) | Thử kết nối thật sớm. Đặt lịch gọi nhẹ định kỳ để tránh tạm dừng |
| A6 | Nhà cung cấp email chưa chốt. Render chặn cổng SMTP | Có tên miền thì dùng Resend qua API; chưa có thì cân nhắc dịch vụ HTTP API khác. Viết `IEmailSender` để đổi dễ |
| A7 | Redis (đã chọn Upstash) là bắt buộc cho luồng đăng ký | Cache board và backplane SignalR để tùy chọn, tắt ở bản demo vì tốn hạn mức lệnh |
| A9 | Hạn mức lệnh gói miễn phí Upstash (khoảng 500.000 lệnh mỗi tháng) có thể cạn nếu `/health` gọi Redis mỗi vài giây | Nhớ kết quả kiểm tra Redis 60 giây; theo dõi lượng lệnh; Staging và Production dùng chung thì thêm tiền tố khóa |
| A8 | Hạn mức miễn phí của Cloudinary | Kiểm tra khi triển khai |

## B. Sau bản 1.0

| # | Vấn đề | Gợi ý |
|---|---|---|
| B1 | Thùng rác và khôi phục dữ liệu xóa mềm (FR-WS-11, BR-GEN-10, đã mô tả nhưng ở mức P3) | Làm ở v1.1 |
| B2 | Xóa tài khoản có thời gian cân nhắc 7 ngày | Làm ở bản sau |
| B3 | Thông báo lưu lâu dài khi bị đổi vai trò, bị xóa khỏi workspace hoặc workspace bị xóa | Bản 1.0 chỉ dùng sự kiện realtime và nhật ký |
| B4 | Universal link và app link trên tên miền riêng cho deep link Mobile | Sau khi có tên miền |
| B5 | Phương án lưu "đăng ký chờ xác thực" ở bảng riêng thay vì tạo User chưa xác thực | Chỉ cân nhắc nếu bảng User bị nhiều bản ghi rác |
| B6 | Người tạo workspace không bị Owner khác hạ hoặc xóa (nếu thấy rủi ro "đảo chính") | Hiện giữ quyền ngang nhau như Trello |

## C. Tiến độ

| # | Vấn đề | Gợi ý |
|---|---|---|
| C1 | Phạm vi đã tăng nhưng lịch 14 + 7 ngày giữ nguyên | Nếu trễ, cắt theo thứ tự: chat 1-1, Owner Note và NoteUpdated, thông báo bình luận, xóa tài khoản (đã ghi trong mục 12.2) |

## D. Cần kiểm chứng khi làm

- Tìm kiếm không dấu với chữ đ: viết unit test, nếu `unaccent` không đổi đ/Đ thì thêm `translate`.
- Gửi email OTP và độ tin cậy của dịch vụ email gói miễn phí.
- Các con số mặc định (OTP 10 phút, chat 60 tin mỗi phút, 300 yêu cầu mỗi phút, quét tài khoản chưa xác thực mỗi 15 phút) đặt trong cấu hình, chỉnh sau khi chạy thử.
