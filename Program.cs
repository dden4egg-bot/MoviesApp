using MoviesApp;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------- 1. Read JSON ----------
string jsonPath = "movies.json";
string jsonText = File.ReadAllText(jsonPath);

// ---------- 2. Deserialize into objects ----------
var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var data = JsonSerializer.Deserialize<JsonRoot>(jsonText, options)
           ?? throw new Exception("Failed to read JSON");

Console.WriteLine($"[JSON] Loaded: directors = {data.Directors.Count}, movies = {data.Movies.Count}");

// ---------- 3-5. Context + DB creation + seeding ----------
using var db = new MoviesContext();

db.Database.EnsureDeleted();      // educational project — recreate every run
db.Database.EnsureCreated();      // creates DB and tables from the model

// Populate Directors
var directors = data.Directors
    .Select(d => new Director { Name = d.Name, BirthYear = d.BirthYear })
    .ToList();
db.Directors.AddRange(directors);
db.SaveChanges();

// Populate Movies — link to Director by name
var directorsByName = directors.ToDictionary(d => d.Name);

foreach (var jm in data.Movies)
{
    if (!directorsByName.TryGetValue(jm.Director, out var director))
    {
        Console.WriteLine($"[WARN] Director \"{jm.Director}\" not found — skipping movie \"{jm.Title}\"");
        continue;
    }

    db.Movies.Add(new Movie
    {
        Title = jm.Title,
        Year = jm.Year,
        DirectorId = director.Id
    });
}
db.SaveChanges();

Console.WriteLine("[DB] movies.db created and populated\n");

// ============================================================
// 6. LINQ queries
// ============================================================
Console.WriteLine("========== LINQ QUERIES ==========");

// 6.1 All movies sorted by year
Console.WriteLine("\n[LINQ 1] All movies sorted by year:");
var allMovies = db.Movies
    .Include(m => m.Director)                  // eager load director
    .OrderBy(m => m.Year)
    .ToList();
foreach (var m in allMovies)
    Console.WriteLine("  " + m);

// 6.2 Movies released after 2000
Console.WriteLine("\n[LINQ 2] Movies released after 2000:");
var modernMovies = db.Movies
    .Include(m => m.Director)
    .Where(m => m.Year > 2000)
    .ToList();
foreach (var m in modernMovies)
    Console.WriteLine("  " + m);

// 6.3 Directors born before 1970 with their movie count
Console.WriteLine("\n[LINQ 3] Directors born before 1970 + movie count:");
var oldDirectors = db.Directors
    .Where(d => d.BirthYear < 1970)
    .Select(d => new { d.Name, d.BirthYear, Count = d.Movies.Count })
    .ToList();
foreach (var d in oldDirectors)
    Console.WriteLine($"  {d.Name} ({d.BirthYear}) — movies: {d.Count}");

// 6.4 Number of movies per director
Console.WriteLine("\n[LINQ 4] Movies per director:");
var counts = db.Movies
    .GroupBy(m => m.Director!.Name)
    .Select(g => new { Director = g.Key, Count = g.Count() })
    .ToList();
foreach (var c in counts)
    Console.WriteLine($"  {c.Director}: {c.Count}");

// 6.5 The newest movie
Console.WriteLine("\n[LINQ 5] Newest movie:");
var newest = db.Movies
    .Include(m => m.Director)
    .OrderByDescending(m => m.Year)
    .FirstOrDefault();
Console.WriteLine("  " + (newest?.ToString() ?? "no data"));

// 6.6 Average release year
Console.WriteLine("\n[LINQ 6] Average movie release year:");
double avgYear = db.Movies.Average(m => m.Year);
Console.WriteLine($"  {avgYear:F2}");


// ============================================================
// 6. SQL queries (raw SQL)
// ============================================================
Console.WriteLine("\n\n========== SQL QUERIES ==========");

// 6.7 All movies by Christopher Nolan (raw SQL)
Console.WriteLine("\n[SQL 1] Movies by Christopher Nolan:");
var nolanMovies = db.Movies
    .FromSqlRaw(@"
        SELECT m.* FROM Movies m
        JOIN Directors d ON m.DirectorId = d.Id
        WHERE d.Name = {0}", "Christopher Nolan")
    .Include(m => m.Director)
    .ToList();
foreach (var m in nolanMovies)
    Console.WriteLine("  " + m);

// 6.8 Top 3 oldest movies (raw SQL)
Console.WriteLine("\n[SQL 2] Top 3 oldest movies:");
var oldest = db.Movies
    .FromSqlRaw("SELECT * FROM Movies ORDER BY Year ASC LIMIT 3")
    .Include(m => m.Director)
    .ToList();
foreach (var m in oldest)
    Console.WriteLine("  " + m);

// 6.9 Directors that have at least one movie (raw SQL, ADO.NET style)
Console.WriteLine("\n[SQL 3] Directors with at least one movie (raw reader):");
using (var conn = db.Database.GetDbConnection())
{
    conn.Open();
    using var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        SELECT d.Name, COUNT(m.Id) AS Cnt
        FROM Directors d
        LEFT JOIN Movies m ON m.DirectorId = d.Id
        GROUP BY d.Id, d.Name
        HAVING Cnt > 0";
    using var reader = cmd.ExecuteReader();
    while (reader.Read())
        Console.WriteLine($"  {reader.GetString(0)}: {reader.GetInt32(1)}");
}