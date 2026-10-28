// Applies shared size, extension, naming, and private-path checks to uploaded files.
namespace Staybnb.Web.Services;

public class FileStorageService(IWebHostEnvironment environment)
{
    // Both public images and private documents use the assignment's 5 MB limit.
    private const long MaximumFileSize = 5 * 1024 * 1024;

    // Saves public property images below wwwroot so they can be displayed by the browser.
    public async Task<string> SaveAsync(IFormFile file, string folder, string[] allowedExtensions)
    {
        if (file.Length == 0 || file.Length > MaximumFileSize)
        {
            throw new InvalidOperationException("The file must be between 1 byte and 5 MB.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"Allowed file types: {string.Join(", ", allowedExtensions)}.");
        }

        var safeFolder = Path.Combine(environment.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(safeFolder);
        // A generated name avoids trusting or overwriting the name supplied by the user.
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(safeFolder, storedName);
        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream);
        return $"/uploads/{folder}/{storedName}";
    }

    // Saves identity documents outside wwwroot so they require an authorised download action.
    public async Task<string> SavePrivateAsync(IFormFile file, string folder, string[] allowedExtensions)
    {
        if (file.Length == 0 || file.Length > MaximumFileSize)
            throw new InvalidOperationException("The file must be between 1 byte and 5 MB.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            throw new InvalidOperationException($"Allowed file types: {string.Join(", ", allowedExtensions)}.");

        var safeFolder = Path.Combine(environment.ContentRootPath, "App_Data", folder);
        Directory.CreateDirectory(safeFolder);
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(safeFolder, storedName);
        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream);
        return Path.Combine(folder, storedName);
    }

    // Rejects paths that attempt to leave the private App_Data directory.
    public string GetPrivateFullPath(string relativePath)
    {
        var dataRoot = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data"));
        var fullPath = Path.GetFullPath(Path.Combine(dataRoot, relativePath));
        if (!fullPath.StartsWith(dataRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid document path.");
        return fullPath;
    }
}
