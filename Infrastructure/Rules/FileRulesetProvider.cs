using ProcessEngine.Worker.Domain.Rules;
using System;
using System.IO;
using System.Text.Json;

namespace ProcessEngine.Worker.Infrastructure.Rules;

public class FileRulesetProvider
{
    private readonly string _filePath;
    private Ruleset? _cachedRuleset;

    public FileRulesetProvider(string filePath)
    {
        _filePath = filePath;
    }

    public Ruleset GetRuleset()
    {
        if (_cachedRuleset != null)
            return _cachedRuleset;

        var json = System.IO.File.ReadAllText(_filePath);

        _cachedRuleset = JsonSerializer.Deserialize<Ruleset>(json)
            ?? throw new InvalidOperationException("Invalid ruleset JSON");

        return _cachedRuleset;
    }
}
