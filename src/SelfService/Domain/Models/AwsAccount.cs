namespace SelfService.Domain.Models
{
    public class AwsAccount
    {
        public Guid Id { get; set; }
        public string AccountId { get; set; }
        public string RoleArn { get; set; }
        public string RoleEmail { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
    }
}