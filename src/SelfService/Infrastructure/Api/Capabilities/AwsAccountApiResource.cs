using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class AwsAccountApiResource
{
    public string Id { get; set; }
    public string? AccountId { get; set; }
    public string? RoleEmail { get; set; }
    public string? Namespace { get; set; }
    public string? Status { get; set; }

    [JsonPropertyName("_links")]
    public AwsAccountLinks Links { get; set; }

    public class AwsAccountLinks
    {
        public ResourceLink Self { get; set; }

        public AwsAccountLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public AwsAccountApiResource(string id, string? accountId, string? roleEmail, string? @namespace, string? status, AwsAccountLinks links)
    {
        Id = id;
        AccountId = accountId;
        RoleEmail = roleEmail;
        Namespace = @namespace;
        Status = status;
        Links = links;
    }
}
