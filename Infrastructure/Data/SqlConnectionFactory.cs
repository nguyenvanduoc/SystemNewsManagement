using System.Data;
using Microsoft.Data.SqlClient;

namespace ThaiBeer.Infrastructure.Data;

public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public IDbConnection TaoKetNoi()
    {
        return new SqlConnection(_connectionString);
    }
}
