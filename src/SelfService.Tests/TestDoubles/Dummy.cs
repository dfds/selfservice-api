using Moq;

namespace SelfService.Tests.TestDoubles;

public static class Dummy
{
    public static T  Of<T>() where T : class
    {
        return new Mock<T>().Object;
    } 
    
    public static T  OfWithParameters<T>(params object[] args) where T : class
    {
        return new Mock<T>(args).Object;
    } 
}