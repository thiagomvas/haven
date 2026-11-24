using Npgsql;

namespace Haven.Infrastructure.Security;

/// <summary>
/// Detects an install created before secrets were generated: the database already has EF migrations
/// applied, so its stored secrets were encrypted with the old built-in zero key.
/// </summary>
public static class LegacyInstallProbe
{
    private const int MaxAttempts = 30;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    public static bool IsExistingInstall(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new SecretsConfigurationException("ConnectionStrings:DefaultConnection is not configured.");

        Exception? last = null;
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                using var connection = new NpgsqlConnection(connectionString);
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText =
                    "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = '__EFMigrationsHistory')";
                return (bool)command.ExecuteScalar()!;
            }
            catch (Exception ex) when (ex is NpgsqlException or System.Net.Sockets.SocketException or TimeoutException)
            {
                // The database container may still be starting; wait rather than mistake this for a fresh install.
                last = ex;
                Thread.Sleep(RetryDelay);
            }
        }

        throw new SecretsConfigurationException(
            $"Could not reach the database to decide how to initialise secrets: {last?.Message}");
    }
}
