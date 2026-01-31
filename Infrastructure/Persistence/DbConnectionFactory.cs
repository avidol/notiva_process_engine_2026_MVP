using System.Data;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace ProcessEngine.Worker.Infrastructure.Persistence;

public class DbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("MySql")!;
    }

    public IDbConnection Create()
        => new MySqlConnection(_connectionString);
}
