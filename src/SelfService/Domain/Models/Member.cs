namespace SelfService.Domain.Models
{
    public class Member
    {
        public string UPN { get; set; }
        public string Email { get; set; }
        public List<Membership> Memberships { get; set; } = new();
    }
}