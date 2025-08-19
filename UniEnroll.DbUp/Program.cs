

using DbUp;


var conn = Environment.GetEnvironmentVariable("DB_CONNECTION")
          ?? "Server=LAPTOP-T2AEOQQQ;Database=UniEnroll;Integrated Security=True;TrustServerCertificate=True;";

EnsureDatabase.For.SqlDatabase(conn);

Console.WriteLine($"Using connection: {conn}");

var upgrader = DeployChanges.To
    .SqlDatabase(conn)
    .WithVariables(new Dictionary<string, string>
    {
        // tweak if you rename the DB in docker-compose later
        { "DatabaseName", "UniEnroll" },
        { "NowUtc", DateTime.UtcNow.ToString("O") }
    })
    .WithScriptsFromFileSystem(Path.Combine(AppContext.BaseDirectory, "Scripts"))
    .LogToConsole()
    .Build();

var result = upgrader.PerformUpgrade();

if (!result.Successful)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(result.Error);
    Console.ResetColor();
    return 1;
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("DbUp success.");
Console.ResetColor();
return 0;

