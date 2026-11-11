using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class AwsAccountsApiResource
{
    public List<AwsAccountItemApiResource> Accounts { get; set; }
    public int AccountLimit { get; set; }

    [JsonPropertyName("_links")]
    public AwsAccountsLinks Links { get; set; }

    public class AwsAccountsLinks
    {
        public ResourceLink Self { get; set; }
        public ResourceLink RequestAccount { get; set; }

        public AwsAccountsLinks(ResourceLink self, ResourceLink requestAccount)
        {
            Self = self;
            RequestAccount = requestAccount;
        }
    }

    public AwsAccountsApiResource(List<AwsAccountItemApiResource> accounts, int accountLimit, AwsAccountsLinks links)
    {
        Accounts = accounts;
        AccountLimit = accountLimit;
        Links = links;
    }
}

public class AwsAccountItemApiResource
{
    public string Id { get; set; }
    public string? Environment { get; set; }
    public string? AccountId { get; set; }
    public string? RoleEmail { get; set; }
    public string? Status { get; set; }

    [JsonPropertyName("_links")]
    public AwsAccountItemLinks Links { get; set; }

    public class AwsAccountItemLinks
    {
        public ResourceLink Self { get; set; }

        public AwsAccountItemLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public AwsAccountItemApiResource(
        string id,
        string? environment,
        string? accountId,
        string? roleEmail,
        string? status,
        AwsAccountItemLinks links
    )
    {
        Id = id;
        Environment = environment;
        AccountId = accountId;
        RoleEmail = roleEmail;
        Status = status;
        Links = links;
    }
}
