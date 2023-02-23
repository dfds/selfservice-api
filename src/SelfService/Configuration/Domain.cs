using SelfService.Domain;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Configuration;

public static class Domain
{
    public static void AddDomain(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(_ => SystemTime.Default);
        builder.Services.AddTransient<ICapabilityRepository, CapabilityRepository>();
        builder.Services.AddTransient<IMembershipRepository, MembershipRepository>();
    }
}