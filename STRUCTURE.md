# TaskFlow Backend: cấu trúc dự án

Theo cách tổ chức của dự án Tutora: 4 project ở thư mục gốc, Database First, không dùng CQRS/MediatR, logic nghiệp vụ nằm trong `Services`.

```
TaskFlow.slnx
schema.sql                    script database (nguồn sự thật)
TaskFlow.Domain/
├── Entities/                 entity sinh từ database bằng scaffold
├── DTO/
│   ├── RequestModel/         dữ liệu client gửi lên
│   └── ResponseModel/        dữ liệu trả về client
├── Enums/                    Role, Priority, NotificationType...
├── Exceptions/               lỗi nghiệp vụ (NotFound, Forbidden, Conflict...)
└── Constants/                hằng số và giới hạn nghiệp vụ (số thành viên, số cột...)
TaskFlow.Application/
├── RepositoryInterfaces/     interface repository (ICardRepository, IUnitOfWork...)
├── ServiceInterfaces/        interface service (ICardService, IEmailSender...)
└── Services/                 logic nghiệp vụ (CardService, WorkspaceService...)
TaskFlow.Infrastructure/
├── DBContext/                AppDbContext (scaffold sinh ra)
├── Repositories/             cài đặt repository
├── Services/                 service kỹ thuật dùng DB/JWT (TokenService...)
└── ExternalServices/         dịch vụ bên ngoài: Redis, Resend (email), Cloudinary, FCM
TaskFlow.Presentation/
├── Controllers/              API controller
├── Hubs/                     SignalR hub
├── Middlewares/              bắt lỗi, ghi log request
├── Filters/                  bộ lọc cho controller
├── Program.cs
└── appsettings*.json
```

## Phụ thuộc giữa các project

```
Presentation ──► Application ──► Domain
      └──► Infrastructure ──► Application, Domain
```

Domain không tham chiếu project nào. Application chỉ biết Domain và dùng interface. Infrastructure cài đặt các interface đó. Presentation nối tất cả lại ở `Program.cs`.

## Lệnh scaffold (Database First)

Chạy ở thư mục gốc, thay `<mật khẩu>`:

```
dotnet ef dbcontext scaffold "Host=localhost;Port=5432;Database=FlowTask;Username=postgres;Password=<mật khẩu>" Npgsql.EntityFrameworkCore.PostgreSQL --project TaskFlow.Infrastructure --startup-project TaskFlow.Presentation --context AppDbContext --context-dir DBContext --context-namespace TaskFlow.Infrastructure.DBContext --output-dir ../TaskFlow.Domain/Entities --namespace TaskFlow.Domain.Entities --no-onconfiguring --force
```

Không dùng EF Migrations: đổi database bằng script SQL rồi scaffold lại với `--force`.

## Việc scaffold không tự làm

- `row_version` ở `cards`, `comments`, `messages`: cần khai báo là concurrency token.
- Cột text enum (`role`, `priority`...): muốn dùng enum C# thì thêm value converter.
- Chỉ mục biểu thức (email không phân biệt hoa thường, tìm kiếm không dấu): vẫn nằm trong database, EF chỉ không biết đến.
