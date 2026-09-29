namespace MoviesApp;

// ============ Database entities ============

public class Director
{
    public int Id { get; set; }                     // Primary key
    public string Name { get; set; } = "";
    public int BirthYear { get; set; }

    // Navigation property: one director has many movies
    public List<Movie> Movies { get; set; } = new();

    public override string ToString() => $"{Name} ({BirthYear})";
}

public class Movie
{
    public int Id { get; set; }                     // Primary key
    public string Title { get; set; } = "";
    public int Year { get; set; }

    // Foreign key
    public int DirectorId { get; set; }

    // Navigation property
    public Director? Director { get; set; }

    public override string ToString() => $"\"{Title}\" ({Year}) - {Director?.Name}";
}

// ============ DTOs for JSON ============

public class JsonDirector
{
    public string Name { get; set; } = "";
    public int BirthYear { get; set; }
}

public class JsonMovie
{
    public string Title { get; set; } = "";
    public int Year { get; set; }
    public string Director { get; set; } = "";   // only name — will resolve to Director via FK
}

public class JsonRoot
{
    public List<JsonDirector> Directors { get; set; } = new();
    public List<JsonMovie> Movies { get; set; } = new();
}