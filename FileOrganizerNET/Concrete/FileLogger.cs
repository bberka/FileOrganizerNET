using FileOrganizerNET.Contracts;

namespace FileOrganizerNET.Concrete;

public class FileLogger : IFileLogger
{
    private readonly string _logFilePath;

    public FileLogger()
    {
        _logFilePath = Path.Combine(AppContext.BaseDirectory, "organizer.log");

        var logDirectory = Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory)) Directory.CreateDirectory(logDirectory);

        var header = $"--- Log session started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ---";
        File.AppendAllText(_logFilePath, Environment.NewLine + header + Environment.NewLine);
    }

    public void Log(string message)
    {
        Console.WriteLine(message);
        File.AppendAllText(_logFilePath, message + Environment.NewLine);
    }
}