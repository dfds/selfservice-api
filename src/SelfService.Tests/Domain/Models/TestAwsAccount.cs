using SelfService.Domain.Models;

namespace SelfService.Tests.Domain.Models;

public class TestAwsAccount
{
    [Fact]
    public void requested_account_is_not_registered_nor_linked_to_kubernetes()
    {
        var account = AwsAccount.RequestNew(CapabilityId.Parse("foo"), DateTime.Today, "bar");

        Assert.Equal(AwsAccountRegistration.Incomplete, account.Registration);
        Assert.Equal(KubernetesLink.Unlinked, account.KubernetesLink);
    }

    [Fact]
    public void registered_account_as_expected()
    {
        var account = AwsAccount.RequestNew(CapabilityId.Parse("foo"), DateTime.Today, "bar");

        account.RegisterRealAwsAccount(RealAwsAccountId.Empty, "foo@foo.com", DateTime.Today);

        Assert.Equal(
            new AwsAccountRegistration(RealAwsAccountId.Empty, "foo@foo.com", DateTime.Today),
            account.Registration
        );
        Assert.Equal(KubernetesLink.Unlinked, account.KubernetesLink);
    }

    [Fact]
    public void linked_to_kubernetes_as_expected()
    {
        var account = AwsAccount.RequestNew(CapabilityId.Parse("foo"), DateTime.Today, "bar");

        account.RegisterRealAwsAccount(RealAwsAccountId.Empty, "foo@foo.com", DateTime.Today);
        account.LinkKubernetesNamespace("dummy-namespace", DateTime.Today);

        Assert.Equal(
            new AwsAccountRegistration(RealAwsAccountId.Empty, "foo@foo.com", DateTime.Today),
            account.Registration
        );
        Assert.Equal(new KubernetesLink("dummy-namespace", DateTime.Today), account.KubernetesLink);
    }

    [Fact]
    public void new_account_has_expected_status()
    {
        var account = AwsAccount.RequestNew(CapabilityId.Parse("foo"), DateTime.Today, "bar");

        Assert.Equal(AwsAccountStatus.Requested, account.Status);
    }

    [Fact]
    public void registered_account_has_expected_status()
    {
        var account = AwsAccount.RequestNew(CapabilityId.Parse("foo"), DateTime.Today, "bar");

        account.RegisterRealAwsAccount(RealAwsAccountId.Empty, "foo@foo.com", DateTime.Today);

        Assert.Equal(AwsAccountStatus.Pending, account.Status);
    }

    [Fact]
    public void linked_account_has_expected_status()
    {
        var account = AwsAccount.RequestNew(CapabilityId.Parse("foo"), DateTime.Today, "bar");

        account.RegisterRealAwsAccount(RealAwsAccountId.Empty, "foo@foo.com", DateTime.Today);
        account.LinkKubernetesNamespace("dummy-namespace", DateTime.Today);

        Assert.Equal(AwsAccountStatus.Completed, account.Status);
    }
}
