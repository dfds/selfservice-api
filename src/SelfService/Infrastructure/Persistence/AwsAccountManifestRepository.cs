using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using SelfService.Domain.Models;
using Signature = LibGit2Sharp.Signature;

namespace SelfService.Infrastructure.Persistence;

public class AwsAccountManifestRepository : IAwsAccountManifestRepository
{
    private Repository _repository;
    private Signature _signature;
    private AwsAccountManifestRepositoryConfig _config;

    public AwsAccountManifestRepository(AwsAccountManifestRepositoryConfig awsAccountManifestRepositoryConfig)
    {
        _config = awsAccountManifestRepositoryConfig;
        Init();
        _repository = new Repository(_config.TemporaryRepoPath);
        _signature = new Signature(_config.GitUsername, _config.GitEmail, DateTimeOffset.Now);
        GetAll();
    }

    void Init()
    {
        if (_config.RemoteRepoUri == "" || _config.TemporaryRepoPath == "")
        {
            throw new Exception("Missing necessary configuration for AwsAccountManifestRepository, can't proceed");
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

    public Task Add(AwsAccountManifest manifest)
    {
        Directory.CreateDirectory($"{_config.TemporaryRepoPath}/{manifest.AwsAccount?.Id}");
        var manifestString = manifest.GenerateManifestString(ExtractTemplateText());
        if (!File.Exists($"{_config.TemporaryRepoPath}/{manifest.AwsAccount?.Id}/terragrunt.hcl"))
        {
            File.WriteAllText($"{_config.TemporaryRepoPath}/{manifest.AwsAccount?.Id}/terragrunt.hcl", manifestString);
            Commands.Stage(_repository, manifest.AwsAccount?.Id.ToString());
            _repository.Commit(
                $"Added new AWS Account for {manifest.Capability?.Id}", // no environment yet
                _signature,
                _signature,
                new CommitOptions()
            );
            var result = CommandExecutor.Run("git", "push", _config.TemporaryRepoPath, 60000);
            result.ThrowIfError();
        }

        return Task.CompletedTask;
    }

    public String ExtractTemplateText()
    {
        return File.ReadAllText($"{_config.TemporaryRepoPath}/template.hcl");
    }
}

public class AwsAccountManifestRepositoryConfig
{
    public String TemporaryRepoPath { get; set; }
    public String RemoteRepoUri { get; set; }
    public String GitUsername { get; set; }
    public String GitEmail { get; set; }
    public String Branch { get; set; }

    public AwsAccountManifestRepositoryConfig(IConfiguration configuration)
    {
        TemporaryRepoPath = configuration.GetValue<String>("SS_AWS_ACCOUNT_TEMPORARY_REPO_PATH") ?? "";
        RemoteRepoUri = configuration.GetValue<String>("SS_AWS_ACCOUNT_REMOTE_REPO_URI") ?? "";
        GitUsername = configuration.GetValue<String>("SS_AWS_ACCOUNT_GIT_USERNAME") ?? "selfservice-api";
        GitEmail = configuration.GetValue<String>("SS_AWS_ACCOUNT_GIT_EMAIL") ?? "ssu@dfds.cloud";
        Branch = configuration.GetValue<String>("SS_AWS_ACCOUNT_GIT_BRANCH") ?? "master";
    }
}

public class NoOpAwsAccountManifestRepository : IAwsAccountManifestRepository
{
    public HashSet<string> GetAll()
    {
        return new HashSet<string>();
    }

    public Task Add(AwsAccountManifest manifest)
    {
        return Task.CompletedTask;
    }
}

public class AwsAccountManifest
{
    public String Path { get; set; }
    public AwsAccount? AwsAccount { get; set; }
    public Capability? Capability { get; set; }

    public AwsAccountManifest()
    {
        Path = "";
    }

    public String GenerateManifestString(string template)
    {
        template = template.Replace("ACCOUNTNAME", Capability?.Id.ToString() ?? "");
        template = template.Replace("CONTEXT_ID", AwsAccount?.Id.ToString() ?? "");
        template = template.Replace("CORRELATION_ID", "");
        template = template.Replace("CAPABILITY_ROOT_ID", Capability?.Id.ToString() ?? "");
        template = template.Replace("CONTEXT_NAME", "default");
        template = template.Replace("CAPABILITY_ID", Capability?.Id.ToString() ?? "");
        template = template.Replace("CAPABILITY_NAME", Capability?.Name ?? "");
        template = template.Replace("ACCOUNT_ID", AwsAccount?.Id.ToString() ?? "");

        return template;
    }
}
