namespace SelfService.Domain
{
    public class Membership
    {
        public Guid Id { get; set; }
        public Member? Member { get; set; }
    }
}