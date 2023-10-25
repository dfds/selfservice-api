using SelfService.Domain.Models;

namespace SelfService.Domain.Services;

public interface IConfluentCloudClientService
{
    Task<string> CreateServiceAccount(string name, string description);
    Task<ListApiKeysResponse?> ListApiKeys(string serviceAccountId, string resourceId);
    Task<ApiKey?> CreateApiKey(string serviceAccountId, string resourceId, string description);
    void DeleteApiKey(string apikeyId);
    void CreateServiceAccountRoleBinding(string serviceAccountId, string resourceId, string roleName);
    void CreateTopic(KafkaClusterId clusterId, string topicName, int partitions, long retention);
    void DeleteTopic(KafkaClusterId clusterId, string topicName);
    void GetConfluentCloudInternalUsers();
    void RegisterTopicSchema(KafkaClusterId clusterId, string topicName, string schema);
    void DeleteTopicSchema(KafkaClusterId clusterId, string topicName, string version);
    Task CreateAclEntries(KafkaClusterId clusterId, CreateAclRequestData[] aclEntries);
}
