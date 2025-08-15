namespace FileOrganizerNET;

public interface IFileLogger
{
    void Log(string message);
}

public class FileLogger : IFileLogger
{
    private readonly string? _logFilePath;

    public FileLogger(string? logFilePath)
    {
        _logFilePath = logFilePath;
        if (string.IsNullOrWhiteSpace(_logFilePath)) return;
        var header = $"--- Log session started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ---";
        File.AppendAllText(_logFilePath, Environment.NewLine + header + Environment.NewLine);
    }

    public void Log(string message)
    {
        Console.WriteLine(message);
        if (!string.IsNullOrWhiteSpace(_logFilePath))
        {
            File.AppendAllText(_logFilePath, message + Environment.NewLine);
        }
    }
}