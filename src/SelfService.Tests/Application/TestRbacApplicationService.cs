using SelfService.Application;

namespace SelfService.Tests.Application;

public class TestRbacApplicationService
{

    [Fact]
    public Task Baseline()
    {
        var rbacSvc = RbacApplicationService.BootstrapTestService();

        var cases = new List<PermittedResponse>
        {
            rbacSvc.IsUserPermitted("emcla@dfds.com", [new Permission { Namespace = "topics", Name = "create"}], "sandbox-emcla-pmyxn"),
            rbacSvc.IsUserPermitted("emcla@dfds.com", [new Permission { Namespace = "topics", Name = "create"}, new Permission { Namespace = "topics", Name = "read-private"}], "sandbox-emcla-pmyxn"),
            rbacSvc.IsUserPermitted("emcla@dfds.com", [new Permission { Namespace = "topics", Name = "read-public"}], "sandbox-emcla-pmyxn"),
            rbacSvc.IsUserPermitted("andfris@dfds.com", [new Permission { Namespace = "topics", Name = "read-private"}], "sandbox-emcla-pmyxn"),
            rbacSvc.IsUserPermitted("andfris@dfds.com", [new Permission { Namespace = "topics", Name = "read-private"}], "andfris-sandbox-6-aeyex"),
        };
        
        cases.ForEach(c => Console.WriteLine(c.Permitted()));
        
        
        return Task.CompletedTask;
    }
}