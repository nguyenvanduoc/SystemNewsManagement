namespace ThaiBeer.Core.Domain.Common;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime? NgayCapNhat { get; set; }
}
