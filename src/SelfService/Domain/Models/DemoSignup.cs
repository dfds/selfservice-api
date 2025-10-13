using System;

namespace SelfService.Domain.Models;

public class DemoSignup
{
    public DemoSignup(
        string email,
        string name
    )
    {
        Email = email;
        Name = name;
    }

    public string Email { get; set; }
    public string Name { get; set; }
}
