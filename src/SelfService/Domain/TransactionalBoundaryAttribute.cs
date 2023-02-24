namespace SelfService.Domain;

[AttributeUsage(AttributeTargets.Method)]
public class TransactionalBoundaryAttribute : Attribute
{

}