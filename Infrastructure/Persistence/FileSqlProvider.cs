using System.IO;

public class FileSqlProvider : ISqlProvider
{
    private readonly string _basePath;

    public FileSqlProvider(string basePath)
    {
        _basePath = basePath;
    }

    public string Get(string name)
    {
        var path = Path.Combine(_basePath, name);
        return File.ReadAllText(path);
    }
}