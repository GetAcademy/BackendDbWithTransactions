using Dapper;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseHttpsRedirection();

var dbPath = Path.Combine(AppContext.BaseDirectory, "data", "app.db");
var connectionString = $"Data Source={dbPath}";

EnsureCounterTables(connectionString);


// POST /counter/increment
// Naiv logikk: les -> +1 -> vent -> skriv
app.MapPost("/counter/increment", async (CounterIncrement input) =>
{
    if (string.IsNullOrWhiteSpace(input.who))
        return Results.BadRequest("who kan ikke være tom");

    using var connection = new SqliteConnection(connectionString);
    await connection.OpenAsync();

    using var transaction = connection.BeginTransaction();

    try
    {
        // 1) Les teller
        var counter = await connection.QuerySingleAsync<int>(
            "SELECT Value FROM counter WHERE Id = @Id",
            new { Id = 1 },
            transaction);

        var newValue = counter + 1;

        await Task.Delay(2000);

        // 2) Oppdater teller
        await connection.ExecuteAsync(
            "UPDATE counter SET Value = @NewValue WHERE Id = @Id",
            new { Id = 1, NewValue = newValue },
            transaction);

        // 3) Lagre historikk
        await connection.ExecuteAsync(
            "INSERT INTO counter_history(Value, CreatedUtc, Who) VALUES (@NewValue, @Utc, @Who)",
            new { NewValue = newValue, Utc = DateTime.UtcNow.ToString("O"), Who = input.who },
            transaction);

        transaction.Commit();
        return Results.Ok(new { message = "Teller oppdatert", value = newValue });
    }
    catch(Exception ex)
    {
        try
        {
            transaction.Rollback();
        }
        catch { /* rollback kan feile hvis DB er ødelagt */ }
        return Results.Problem(
            detail: ex.Message,
            title: "Kunne ikke oppdatere teller",
            statusCode: 500
        );
    }
});

// (Valgfritt, men veldig nyttig i timen) – se status + siste historikk
app.MapGet("/counter", async () =>
{
    using var connection = new SqliteConnection(connectionString);

    var value = await connection.ExecuteScalarAsync<long>(
        "SELECT value FROM counter WHERE id = 1;"
    );

    var history = (await connection.QueryAsync(@"
        SELECT who, value, createdUtc
        FROM counter_history
        ORDER BY id DESC
        LIMIT 20;
    ")).ToList();

    return Results.Ok(new { value, history });
});

app.Run();

void EnsureCounterTables(string connectionString)
{
    using var connection = new SqliteConnection(connectionString);

    connection.Execute(@"
        CREATE TABLE IF NOT EXISTS counter (
            id INTEGER PRIMARY KEY,
            value INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS counter_history (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            who TEXT NOT NULL,
            value INTEGER NOT NULL,
            createdUtc TEXT NOT NULL
        );
    ");

    // Sørg for at telleren har én rad
    connection.Execute(@"
        INSERT INTO counter (id, value)
        SELECT 1, 0
        WHERE NOT EXISTS (SELECT 1 FROM counter WHERE id = 1);
    ");
}

record CounterIncrement(string who);
