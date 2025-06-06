﻿using SelfService.Domain.Models;

namespace SelfService.Application;

public interface ICapabilityApplicationService
{
    Task<CapabilityId> CreateNewCapability(
        CapabilityId capabilityId,
        string name,
        string description,
        string requestedBy,
        string jsonMetadata,
        int jsonSchemaVersion
    );

    // NOTE [jandr@2023-03-27]: should this be moved to a more topic-centric use-case (e.g. topic application service)?
    Task<KafkaTopicId> RequestNewTopic(
        CapabilityId capabilityId,
        KafkaClusterId kafkaClusterId,
        KafkaTopicName name,
        string description,
        KafkaTopicPartitions partitions,
        KafkaTopicRetention retention,
        string requestedBy
    );

    Task RequestKafkaClusterAccess(CapabilityId capabilityId, KafkaClusterId kafkaClusterId, UserId requestedBy);
    Task RegisterKafkaClusterAccessGranted(CapabilityId capabilityId, KafkaClusterId kafkaClusterId);
    Task RequestCapabilityDeletion(CapabilityId capabilityId, UserId userId);
    Task CancelCapabilityDeletionRequest(CapabilityId capabilityId, UserId userId);
    Task ActOnPendingCapabilityDeletions();
    Task SetJsonMetadata(CapabilityId capabilityId, string jsonMetadata);
    Task<string> GetJsonMetadata(CapabilityId capabilityId);
    Task<bool> DoesOnlyModifyRequiredProperties(string jsonMetadata, CapabilityId capabilityId);
    Task<ConfigurationLevelInfo> GetConfigurationLevel(CapabilityId capabilityId);

    Task<bool> SelfAssessmentOptionExists(CapabilityId capabilityId, SelfAssessmentOptionId selfAssessmentOptionId);
    Task<List<SelfAssessment>> GetAllSelfAssessments(CapabilityId capabilityId);

    Task<SelfAssessmentId> UpdateSelfAssessment(
        CapabilityId capabilityId,
        SelfAssessmentOptionId selfAssessmentOptionId,
        UserId userId,
        SelfAssessmentStatus status
    );
}
