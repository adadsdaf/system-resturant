using System.Data;
using Dapper;
using Npgsql;

namespace RestaurantMS.Data;

public class DbHelper
{
    private readonly string _connectionString;

    public DbHelper(IConfiguration configuration)
    {
        var rawUrl = configuration.GetConnectionString("DefaultConnection") ?? "";
        _connectionString = ConvertPostgresUrl(rawUrl);
    }

    private static string ConvertPostgresUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return "";
        // Handle postgres://user:pass@host:port/db format
        if (url.StartsWith("postgres://") || url.StartsWith("postgresql://"))
        {
            var uri = new Uri(url);
            var userInfo = uri.UserInfo.Split(':');
            var user = userInfo[0];
            var password = userInfo.Length > 1 ? userInfo[1] : "";
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;
            var db = uri.AbsolutePath.TrimStart('/');
            return $"Host={host};Port={port};Database={db};Username={user};Password={password};SSL Mode=Prefer;Trust Server Certificate=true;";
        }
        return url;
    }

    public IDbConnection GetConnection() => new NpgsqlConnection(_connectionString);

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null)
    {
        using var conn = GetConnection();
        return await conn.QueryAsync<T>(sql, param);
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null)
    {
        using var conn = GetConnection();
        return await conn.QueryFirstOrDefaultAsync<T>(sql, param);
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql, object? param = null)
    {
        using var conn = GetConnection();
        return await conn.ExecuteScalarAsync<T>(sql, param);
    }

    public async Task<int> ExecuteAsync(string sql, object? param = null)
    {
        using var conn = GetConnection();
        return await conn.ExecuteAsync(sql, param);
    }

    public async Task<T?> QueryScalarAsync<T>(string sql, object? param = null)
    {
        using var conn = GetConnection();
        return await conn.ExecuteScalarAsync<T>(sql, param);
    }

    public IDbConnection OpenConnection()
    {
        var conn = GetConnection();
        conn.Open();
        return conn;
    }
}
