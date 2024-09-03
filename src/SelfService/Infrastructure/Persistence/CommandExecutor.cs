using System.Diagnostics;

namespace SelfService.Infrastructure.Persistence;

class CommandExecutor
{
    public String? Output { get; set; }
    public String? Error { get; set; }
    public int ExitCode { get; set; }

    public static CommandExecutor Run(String process, String args, String workingDirectory, int timeout)
    {
        var proc = new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = process,
                WorkingDirectory = workingDirectory,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        proc.Start();
        if (timeout == 0)
        {
            proc.WaitForExit();
        }
        else
        {
            proc.WaitForExit(timeout);
        }

        var output = proc.StandardOutput.ReadToEnd();
        var error = proc.StandardError.ReadToEnd();
        return new CommandExecutor
        {
            Output = output,
            Error = error,
            ExitCode = proc.ExitCode
        };
    }

    public void ThrowIfError()
    {
        if (ExitCode != 0)
        {
            throw new CommandErrorException(Error!);
        }
    }
}

public class CommandErrorException : Exception
{
    public String ErrorOutput { get; set; }

    public CommandErrorException(string message)
        : base($"Error encountered while executing command: {message}")
    {
        ErrorOutput = message;
    }
}
