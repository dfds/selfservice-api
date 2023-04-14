using SelfService.Domain.Models;

namespace SelfService.Tests.Domain.Models;

public class TestAwsAccount
{
    [Fact]
    public void requested_account_is_not_registered_nor_completed()
    {
        var account = AwsAccount.RequestNew(CapabilityId.Parse("foo"), DateTime.Today, "bar");

        Assert.Equal(AwsAccountRegistration.Incomplete, account.Registration);
        Assert.Null(account.CompletedAt);
    }

    [Fact]
    public void registered_account_as_expected()
    {
        var account = AwsAccount.RequestNew(CapabilityId.Parse("foo"), DateTime.Today, "bar");

        account.RegisterRealAwsAccount(RealAwsAccountId.Empty, "foo@foo.com", DateTime.Today);

        Assert.Equal(new AwsAccountRegistration(RealAwsAccountId.Empty, "foo@foo.com", DateTime.Today), account.Registration);
        Assert.Null(account.CompletedAt);
    }

    [Fact]
    public void completed_account_as_expected()
    {
        var account = AwsAccount.RequestNew(CapabilityId.Parse("foo"), DateTime.Today, "bar");

        account.RegisterRealAwsAccount(RealAwsAccountId.Empty, "foo@foo.com", DateTime.Today);
        account.Complete(DateTime.Today);

        Assert.Equal(new AwsAccountRegistration(RealAwsAccountId.Empty, "foo@foo.com", DateTime.Today), account.Registration);
        Assert.Equal(DateTime.Today, account.CompletedAt);
    }

    [Fact]
    public void new_account_has_expected_status()
    {
        var account = AwsAccount.RequestNew(CapabilityId.Parse("foo"), DateTime.Today, "bar");

        Assert.Equal(AwsAccountStatus.Pending, account.Status);
    }

    [Fact]
    public void registered_account_has_expected_status()
    {
        var account = AwsAccount.RequestNew(CapabilityId.Parse("foo"), DateTime.Today, "bar");

        account.RegisterRealAwsAccount(RealAwsAccountId.Empty, "foo@foo.com", DateTime.Today);

        Assert.Equal(AwsAccountStatus.Registered, account.Status);
    }

    [Fact]
    public void completed_account_has_expected_status()
    {
        var account = AwsAccount.RequestNew(CapabilityId.Parse("foo"), DateTime.Today, "bar");

        account.RegisterRealAwsAccount(RealAwsAccountId.Empty, "foo@foo.com", DateTime.Today);
        account.Complete(DateTime.Today);

        Assert.Equal(AwsAccountStatus.Completed, account.Status);
    }
}