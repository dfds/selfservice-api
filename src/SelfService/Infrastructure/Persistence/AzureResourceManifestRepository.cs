using System.Diagnostics;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using SelfService.Application;
using SelfService.Domain.Models;
using Signature = LibGit2Sharp.Signature;

namespace SelfService.Infrastructure.Persistence;

public class AzureResourceManifestRepository : IAzureResourceManifestRepository
{
    private Repository _repository;
    private Signature _signature;

    public AzureResourceManifestRepository()
    {
        Init(); 
        _repository = new Repository("/tmp/ssu-azure-rg-manifests");
        _signature = new Signature("selfservice-api", "ssu@dfds.cloud", DateTimeOffset.Now);
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

    public Task Add(AzureResourceManifest manifest)
    {
        Directory.CreateDirectory($"/tmp/ssu-azure-rg-manifests/{manifest.AzureResource?.Id}");
        var manifestString = manifest.GenerateManifestString();
        if (!File.Exists($"/tmp/ssu-azure-rg-manifests/{manifest.AzureResource?.Id}/terragrunt.hcl"))
        {
            File.WriteAllText($"/tmp/ssu-azure-rg-manifests/{manifest.AzureResource?.Id}/terragrunt.hcl", manifestString);
            Commands.Stage(_repository, manifest.AzureResource?.Id.ToString());
            _repository.Commit($"Added new Azure Resource Group for {manifest.Capability?.Id} in environment {manifest.AzureResource?.Environment}", _signature, _signature, new CommitOptions());
        }
        
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

    public String GenerateManifestString()
    {
        return $$"""
                 terraform {
                   source = "git::https://github.com/dfds/azure-infrastructure-modules.git//capability-context?ref=main&depth=1"
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
}

