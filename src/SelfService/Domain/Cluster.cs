namespace SelfService.Domain
{
	public class Cluster
	{
		public Guid Id { get; set; }
		public string? ClusterId { get; set; }
		public string? Name { get; set; }
		public string? Description { get; set; }
		public bool Enabled { get; set; }
	}
}
