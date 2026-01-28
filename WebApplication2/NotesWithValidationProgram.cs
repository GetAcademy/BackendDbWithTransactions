/*
using Dapper;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseHttpsRedirection();

//var dataDir = Path.Combine(builder.Environment.ContentRootPath, "data");
//var dbPath = Path.Combine(dataDir, "app.db");

var connectionString = "Data Source=bin/Debug/net10.0/data/app.db";

using var connection = new SqliteConnection(connectionString);
var sql = @"
CREATE TABLE IF NOT EXISTS notes (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  title TEXT NOT NULL,
  body TEXT NOT NULL,
  createdUtc TEXT NOT NULL
);
";
await connection.ExecuteAsync(sql);

app.MapGet("/notes", async () =>
{
    using var connection = new SqliteConnection(connectionString);
    var sql = "SELECT id, title, body, createdUtc FROM notes ORDER BY id";
    var notes = await connection.QueryAsync<Note>(sql);
    return Results.Ok(notes);
});

app.MapPost("/notes", async (Note input) =>
{
    if (!IsValid(input))
        return Results.BadRequest("Ugyldig input");

    using var connection = new SqliteConnection(connectionString);
    var createdUtc = DateTime.UtcNow.ToString("O"); // ISO-8601
    var sql = @"INSERT INTO notes (title, body, createdUtc)
                VALUES (@Title, @Body, @CreatedUtc);
                SELECT last_insert_rowid();";

    var id = await connection.ExecuteScalarAsync<long>(sql, new
    {
        input.Title,
        input.Body,
        CreatedUtc = createdUtc
    });

    return Results.Created($"/notes/{id}", new { id });
});

app.Run();

bool IsValid(Note note)
{
    return !string.IsNullOrWhiteSpace(note.Title)
           && !string.IsNullOrWhiteSpace(note.Body);
}

record Note(long Id, string Title, string Body, string CreatedUtc);

*/