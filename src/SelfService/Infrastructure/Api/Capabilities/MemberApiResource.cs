﻿namespace SelfService.Infrastructure.Api.Capabilities;

public class MemberApiResource
{
    public string Id { get; set; }
    public string? Name { get; set; }
    public string Email { get; set; }

    public MemberApiResource(string id, string? name, string email)
    {
        Id = id;
        Name = name;
        Email = email;
    }
}
