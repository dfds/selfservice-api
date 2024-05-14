using System.Diagnostics;
using System.Text.RegularExpressions;
using SelfService.Application;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class AzureResourceManifestRepository : IAzureResourceManifestRepository
{
    private LibGit2Sharp.Repository _repository;

    public AzureResourceManifestRepository()
    {
        Init(); 
        _repository = new LibGit2Sharp.Repository("/tmp/ssu-azure-rg-manifests");
        GetAll();
    }

    void Init()
    {
        if (!Directory.Exists("/tmp/ssu-azure-rg-manifests"))
        {
            Directory.CreateDirectory("/tmp/ssu-azure-rg-manifests");
        }

        if (!Directory.Exists("/tmp/ssu-azure-rg-manifests/.git"))
        {
            var result = CommandExecutor.Run("git", "clone git@github.com:dfds/azure-rg-manifests.git .", "/tmp/ssu-azure-rg-manifests", 60000);
        }
        else
        {
            var result = CommandExecutor.Run("git", "fetch origin", "/tmp/ssu-azure-rg-manifests", 60000);
            result = CommandExecutor.Run("git", "reset --hard origin/master", "/tmp/ssu-azure-rg-manifests", 60000);
        }
    }

    public HashSet<String> GetAll()
    {
        var set = new HashSet<String>();

        var directories = Directory.GetDirectories("/tmp/ssu-azure-rg-manifests");
        directories = directories.Select(x => x.Replace("/tmp/ssu-azure-rg-manifests/", "")).ToArray();

        var rg = new Regex("[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}");

        foreach (var dir in directories)
        {
            if (rg.IsMatch(dir))
            {
                set.Add(dir);
            }
        }

        return set;
    }

    public void Add(AzureResourceManifest manifest)
    {
        Directory.CreateDirectory($"/tmp/ssu-azure-rg-manifests/${manifest.AzureResource?.Id}");
    }
}

public class AzureResourceManifest
{
    public String Path { get; set; }
    public AzureResource? AzureResource { get; set; }
    public Capability? Capability { get; set; }

    public AzureResourceManifest()
    {
        Path = "";
    }
}

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
}