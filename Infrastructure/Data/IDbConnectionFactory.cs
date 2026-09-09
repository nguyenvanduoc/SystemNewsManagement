using System.Data;

namespace ThaiBeer.Infrastructure.Data;

public interface IDbConnectionFactory
{
    IDbConnection TaoKetNoi();
}
