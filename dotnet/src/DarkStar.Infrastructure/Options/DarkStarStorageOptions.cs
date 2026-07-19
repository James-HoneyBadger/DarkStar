namespace DarkStar.Infrastructure.Options;

public sealed class DarkStarStorageOptions
{
    public string HomePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".darkstar"
    );
}
