using SelfService.Infrastructure.Messaging;

namespace SelfService.Tests.Infrastructure.Messaging;

public class TestLegacyIncomingMessageFactory
{
    [Fact]
    public void Can_deserialize_events_from_legacy_envelope()
    {
        const string rawMessage = "{\"version\":\"1\",\"eventName\":\"aws_context_account_created\",\"x-correlationId\":\"0a4c0dc1-660e-4c39-bb2b-769bacd29c9e\",\"x-sender\":\"K8sJanitor.WebApi, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null\",\"payload\":{\"contextId\":\"8b8dcf9c-cd7c-44b5-995d-b75448dfd093\",\"capabilityId\":\"0d03e3ad-2118-46b7-970e-0ca87b59a202\",\"capabilityName\":\"Team One\",\"capabilityRootId\":\"team-one\",\"contextName\":\"default\",\"accountId\":\"123456789012\",\"roleArn\":\"arn:some-role\",\"roleEmail\":\"some@email.com\"}}";

        var sut = new ConsumerConfiguration.LegacyIncomingMessageFactory();

        var transportLevelMessage = sut.Create(rawMessage);

        Assert.Equal("0a4c0dc1-660e-4c39-bb2b-769bacd29c9e", transportLevelMessage.Metadata.CorrelationId);
        Assert.Equal("0a4c0dc1-660e-4c39-bb2b-769bacd29c9e", transportLevelMessage.Metadata.MessageId);
        Assert.Equal("0a4c0dc1-660e-4c39-bb2b-769bacd29c9e", transportLevelMessage.Metadata.CausationId);
        Assert.Equal("aws_context_account_created", transportLevelMessage.Metadata.Type);

        var @event = (AwsContextAccountCreated)transportLevelMessage.ReadDataAs(typeof(AwsContextAccountCreated));

        Assert.Equal("8b8dcf9c-cd7c-44b5-995d-b75448dfd093", @event.ContextId);
        Assert.Equal("0d03e3ad-2118-46b7-970e-0ca87b59a202", @event.CapabilityId);
        Assert.Equal("Team One", @event.CapabilityName);
        Assert.Equal("team-one", @event.CapabilityRootId);
        Assert.Equal("default", @event.ContextName);
        Assert.Equal("123456789012", @event.AccountId);
        Assert.Equal("arn:some-role", @event.RoleArn);
        Assert.Equal("some@email.com", @event.RoleEmail);
    }
}