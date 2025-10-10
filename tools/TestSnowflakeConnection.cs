using System;
using System.IO;
using Snowflake.Data.Client;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("Usage: dotnet run <account> <user> <keyfile> <password>");
            return;
        }

        var account = args[0];
        var user = args[1];
        var keyFile = args[2];
        var password = args[3];

        Console.WriteLine($"Testing Snowflake connection...");
        Console.WriteLine($"Account: {account}");
        Console.WriteLine($"User: {user}");
        Console.WriteLine($"Key file: {keyFile}");
        Console.WriteLine();

        try
        {
            // Try using private_key_file parameter (simplest)
            var connString = $"account={account};" +
                           $"user={user};" +
                           $"authenticator=snowflake_jwt;" +
                           $"private_key_file={keyFile};" +
                           $"private_key_pwd={password};";

            Console.WriteLine("Attempting connection with private_key_file parameter...");

            using (var conn = new SnowflakeDbConnection(connString))
            {
                conn.Open();
                Console.WriteLine("✅ SUCCESS! Connected to Snowflake!");

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT CURRENT_USER(), CURRENT_ACCOUNT(), CURRENT_TIMESTAMP()";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine($"User: {reader.GetString(0)}");
                            Console.WriteLine($"Account: {reader.GetString(1)}");
                            Console.WriteLine($"Timestamp: {reader.GetDateTime(2)}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ FAILED: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
        }
    }
}
