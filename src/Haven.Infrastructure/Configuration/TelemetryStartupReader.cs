using System.Text.Json;

using Haven.Application.Configuration;

using Npgsql;

namespace Haven.Infrastructure.Configuration;

public static class TelemetryStartupReader
{
    public static TelemetryOptions Read(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return new TelemetryOptions();

        try
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT value FROM haven_settings WHERE category = @category LIMIT 1";
            cmd.Parameters.AddWithValue("@category", TelemetryOptions.SectionName);

            var json = cmd.ExecuteScalar() as string;
            if (json is null)
                return new TelemetryOptions();

            return JsonSerializer.Deserialize<TelemetryOptions>(json) ?? new TelemetryOptions();
        }
        catch
        {
            return new TelemetryOptions();
        }
    }
}