// 职责：统一蓝图文件命名、列表、重命名及不覆盖源文件的复制规则。
// 入口：GetNames() / TryRename() / TryDuplicate()；不依赖 Unity、管理器或 UI。
using System;
using System.Collections.Generic;
using System.IO;

internal sealed class BlueprintFileStore
{
    private readonly string _directory;
    public BlueprintFileStore(string directory)
    {
        _directory = directory;
    }

    public string GetPath(string name)
    {
        Directory.CreateDirectory(_directory);
        return Path.Combine(_directory, name + ".json");
    }

    public IEnumerable<string> GetNames()
    {
        Directory.CreateDirectory(_directory);
        foreach (string file in Directory.GetFiles(_directory, "*.json"))
        {
            yield return Path.GetFileNameWithoutExtension(file);
        }
    }

    public bool TryRename(string oldName, string newName, out string reason)
    {
        if (!CanUseFileName(newName, out reason))
        {
            return false;
        }

        if (string.Equals(oldName, newName, StringComparison.Ordinal))
        {
            return true;
        }

        string oldPath = GetPath(oldName);
        string newPath = GetPath(newName);
        if (!File.Exists(oldPath))
        {
            reason = $"Blueprint not found: {oldName}";
            return false;
        }

        if (File.Exists(newPath))
        {
            reason = $"Blueprint already exists: {newName}";
            return false;
        }

        try
        {
            File.Move(oldPath, newPath);
            return true;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            reason = exception.Message;
            return false;
        }
    }

    public bool TryDuplicate(string name, out string duplicateName, out string reason)
    {
        duplicateName = null;
        if (!CanUseFileName(name, out reason))
        {
            return false;
        }

        string source = GetPath(name);
        if (!File.Exists(source))
        {
            reason = $"Blueprint not found: {name}";
            return false;
        }

        string baseName = name + " Copy";
        duplicateName = baseName;
        int suffix = 2;
        while (File.Exists(GetPath(duplicateName)))
        {
            duplicateName = $"{baseName} {suffix++}";
        }

        try
        {
            File.Copy(source, GetPath(duplicateName), false);
            return true;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            reason = exception.Message;
            return false;
        }
    }

    private static bool CanUseFileName(string name, out string reason)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            reason = "Name cannot be empty.";
            return false;
        }

        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.EndsWith(" ") || name.EndsWith("."))
        {
            reason = $"Invalid file name: {name}";
            return false;
        }

        reason = string.Empty;
        return true;
    }
}
