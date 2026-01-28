using Dapper;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseHttpsRedirection();

var connectionString = "Data Source=bin/Debug/net10.0/data/app.db";

EnsureCounterTables(connectionString);


// POST /counter/increment
// Naiv logikk: les -> +1 -> vent -> skriv
app.MapPost("/counter/increment", async (CounterIncrement input) =>
{
    if (string.IsNullOrWhiteSpace(input.who))
        return Results.BadRequest("who kan ikke være tom");

    using var connection = new SqliteConnection(connectionString);

    // 1) Les nåværende verdi
    var current = await connection.ExecuteScalarAsync<long>(
        "SELECT value FROM counter WHERE id = 1;"
    );

    // 2) Regn ut ny verdi
    var next = current + 1;

    // 3) Bevisst pause for å gjøre overlap lett å få til
    await Task.Delay(2000);

    // 4) Lagre historikk (hvem -> hvilken verdi)
    await connection.ExecuteAsync(@"
        INSERT INTO counter_history (who, value, createdUtc)
        VALUES (@who, @value, @createdUtc);
    ", new
    {
        who = input.who,
        value = next,
        createdUtc = DateTime.UtcNow.ToString("O")
    });

    // 5) Oppdater telleren (lost update kan skje her)
    await connection.ExecuteAsync(@"
        UPDATE counter SET value = @value WHERE id = 1;
    ", new { value = next });

    return Results.Ok(new { value = next });
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
