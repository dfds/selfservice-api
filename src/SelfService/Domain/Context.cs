namespace SelfService.Domain
{
    public class Context
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? AWSAccountId { get; set; }
        public string? AWSRoleArn { get; set; }
        public string? AWSRoleEmail { get; set; }
    }
}