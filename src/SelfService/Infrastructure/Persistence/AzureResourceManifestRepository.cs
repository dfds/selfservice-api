using System.Diagnostics;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using SelfService.Domain.Models;
using Signature = LibGit2Sharp.Signature;

namespace SelfService.Infrastructure.Persistence;

public class AzureResourceManifestRepository : IAzureResourceManifestRepository
{
    private Repository _repository;
    private Signature _signature;
    private AzureResourceManifestRepositoryConfig _config;

    public AzureResourceManifestRepository(AzureResourceManifestRepositoryConfig azureResourceManifestRepositoryConfig)
    {
        _config = azureResourceManifestRepositoryConfig;
        Init();
        _repository = new Repository(_config.TemporaryRepoPath);
        _signature = new Signature(_config.GitUsername, _config.GitEmail, DateTimeOffset.Now);
        GetAll();
    }

    void Init()
    {
        if (_config.RemoteRepoUri == "" || _config.TemporaryRepoPath == "")
        {
            throw new Exception("Missing necessary configuration for AzureResourceManifestRepository, can't proceed");
        }

        if (!Directory.Exists(_config.TemporaryRepoPath))
        {
            Directory.CreateDirectory(_config.TemporaryRepoPath);
        }

        if (!Directory.Exists($"{_config.TemporaryRepoPath}/.git"))
        {
            var result = CommandExecutor.Run(
                "git",
                $"clone {_config.RemoteRepoUri} .",
                _config.TemporaryRepoPath,
                60000
            );
            result.ThrowIfError();
            result = CommandExecutor.Run("git", $"checkout {_config.Branch}", _config.TemporaryRepoPath, 60000);
            result.ThrowIfError();
        }
        else
        {
            var result = CommandExecutor.Run("git", "fetch origin", _config.TemporaryRepoPath, 60000);
            result.ThrowIfError();
            result = CommandExecutor.Run(
                "git",
                $"reset --hard origin/{_config.Branch}",
                _config.TemporaryRepoPath,
                60000
            );
            result.ThrowIfError();
        }
    }

    public HashSet<String> GetAll()
    {
        var set = new HashSet<String>();

        var directories = Directory.GetDirectories(_config.TemporaryRepoPath);
        directories = directories.Select(x => x.Replace($"{_config.TemporaryRepoPath}/", "")).ToArray();

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

    public Task Add(AzureResourceManifest manifest)
    {
        Directory.CreateDirectory($"{_config.TemporaryRepoPath}/{manifest.AzureResource?.Id}");
        var manifestString = manifest.GenerateManifestString(ExtractManifestRef());
        if (!File.Exists($"{_config.TemporaryRepoPath}/{manifest.AzureResource?.Id}/terragrunt.hcl"))
        {
            File.WriteAllText(
                $"{_config.TemporaryRepoPath}/{manifest.AzureResource?.Id}/terragrunt.hcl",
                manifestString
            );
            Commands.Stage(_repository, manifest.AzureResource?.Id.ToString());
            _repository.Commit(
                $"Added new Azure Resource Group for {manifest.Capability?.Id} in environment {manifest.AzureResource?.Environment}",
                _signature,
                _signature,
                new CommitOptions()
            );
            var result = CommandExecutor.Run("git", "push", _config.TemporaryRepoPath, 60000);
            result.ThrowIfError();
        }

        return Task.CompletedTask;
    }

    public String ExtractManifestRef()
    {
        var templateManifestContent = File.ReadAllText(
            $"{_config.TemporaryRepoPath}/00000000-0000-0000-0000-000000000000/terragrunt.hcl"
        );
        var match = Regex.Match(templateManifestContent, "source =.*ref=(?<ref>[^&^\"\\n\\r]*)");
        return match.Groups["ref"].Value;
    }
}

public class AzureResourceManifestRepositoryConfig
{
    public String TemporaryRepoPath { get; set; }
    public String RemoteRepoUri { get; set; }
    public String GitUsername { get; set; }
    public String GitEmail { get; set; }
    public String Branch { get; set; }

    public AzureResourceManifestRepositoryConfig(IConfiguration configuration)
    {
        TemporaryRepoPath = configuration.GetValue<String>("SS_ARM_TEMPORARY_REPO_PATH") ?? "";
        RemoteRepoUri = configuration.GetValue<String>("SS_ARM_REMOTE_REPO_URI") ?? "";
        GitUsername = configuration.GetValue<String>("SS_ARM_GIT_USERNAME") ?? "selfservice-api";
        GitEmail = configuration.GetValue<String>("SS_ARM_GIT_EMAIL") ?? "ssu@dfds.cloud";
        Branch = configuration.GetValue<String>("SS_ARM_GIT_BRANCH") ?? "master";
    }
}

public class NoOpAzureResourceManifestRepository : IAzureResourceManifestRepository
{
    public HashSet<string> GetAll()
    {
        return new HashSet<string>();
    }

    public Task Add(AzureResourceManifest manifest)
    {
        return Task.CompletedTask;
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

    public String GenerateManifestString(string gitRef)
    {
        var options = "depth=1";
        if (gitRef != "")
        {
            options = $"ref={gitRef}&depth=1";
        }

        return $$"""
                 terraform {
                   source = "git::https://github.com/dfds/azure-infrastructure-modules.git//capability-context?{{options}}"
                 }
                 
                 include {
                   path = "${find_in_parent_folders()}"
                 }
                 
                 inputs = {
                 
                   name = "{{Capability?.Id}}"
                 
                   tribe = "tribename"
                 
                   team  = "teamname"
                   
                   email = "aws.{{Capability?.Id}}@dfds.com"
                 
                   context_id = "{{AzureResource?.Id}}"
                 
                   correlation_id = "f6189c11-c710-40ed-8c79-8f94eb7b04cf"
                 
                   capability_name = "{{Capability?.Name}}"
                 
                   capability_root_id = "{{Capability?.Id}}"
                 
                   context_name = "{{AzureResource?.Environment}}"
                 
                   capability_id = "{{Capability?.Id}}"
                 
                 }
                 """;
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
