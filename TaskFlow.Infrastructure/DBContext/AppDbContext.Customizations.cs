using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.DBContext;

// Phần viết tay của AppDbContext. File này KHÔNG bị lệnh scaffold --force ghi đè
// (scaffold chỉ ghi lại AppDbContext.cs và các entity).
public partial class AppDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // row_version: trigger trong database tăng sau mỗi lần sửa (BR-CRD-09).
        // Đánh dấu là concurrency token để EF thêm "WHERE row_version = <giá trị đã đọc>" khi UPDATE;
        // hai người cùng sửa một dòng thì người sau nhận DbUpdateConcurrencyException (trả 409).
        // ValueGeneratedOnAddOrUpdate để EF đọc lại giá trị mới do trigger sinh ra.
        modelBuilder.Entity<Card>().Property(e => e.RowVersion).IsConcurrencyToken().ValueGeneratedOnAddOrUpdate();
        modelBuilder.Entity<Comment>().Property(e => e.RowVersion).IsConcurrencyToken().ValueGeneratedOnAddOrUpdate();
        modelBuilder.Entity<Message>().Property(e => e.RowVersion).IsConcurrencyToken().ValueGeneratedOnAddOrUpdate();
    }
}
