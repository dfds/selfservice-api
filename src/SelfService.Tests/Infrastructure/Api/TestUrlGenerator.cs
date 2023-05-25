using System.Diagnostics;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;
using SelfService.Infrastructure.Api.Kafka;
using Xunit.Abstractions;

namespace SelfService.Tests.Infrastructure.Api;

public class TestUrlGenerator
{
    private readonly ITestOutputHelper _output;
    private readonly Link _link;

    public TestUrlGenerator(ITestOutputHelper output)
    {
        _output = output;
        _link = new Link(new HttpContextAccessor { HttpContext = new DefaultHttpContext() }, new FakeLinkGenerator());
    }

    [Fact]
    public void GetCapabilities()
    {
        var descriptor = _link.To<CapabilityController>(controller => controller.GetAllCapabilities());

        Assert.Equal($"action={nameof(CapabilityController.GetAllCapabilities)}&" +
                     $"controller=Capability", descriptor);
    }

    [Fact]
    public void GetCapabilityById()
    {
        var capabilityId = CapabilityId.Parse("some-capability-id");
        var descriptor = _link.To<CapabilityController>(controller => controller.GetCapabilityById(capabilityId));

        Assert.Equal($"id=some-capability-id&" +
                     $"action={nameof(CapabilityController.GetCapabilityById)}&" +
                     $"controller=Capability", descriptor);
    }

    [Fact]
    public void AddCapabilityMembershipApplications()
    {
        var capabilityId = CapabilityId.Parse("some-capability-id");
        var descriptor = _link.To<CapabilityController>(controller => controller.AddCapabilityMembershipApplications(capabilityId, null!, null!));

        AssertJson
        
        Assert.Equal("id=some-capability-id&" +
                     $"action={nameof(CapabilityController.AddCapabilityMembershipApplications)}&" +
                     "controller=Capability", descriptor);
    }

    [Fact]
    public void GetSingleMessageContract()
    {
        var kafkaTopicId = KafkaTopicId.New();
        var messageContractId = MessageContractId.New();
        var descriptor = _link.To<KafkaTopicController>(controller => controller.GetSingleMessageContract(kafkaTopicId, messageContractId));

        Assert.Equal($"id={kafkaTopicId}&" +
                     $"contractId={messageContractId}&" +
                     $"action={nameof(KafkaTopicController.GetSingleMessageContract)}&" +
                     $"controller=KafkaTopic", descriptor);
    }

    // [RunnableInDebugOnly]
    // public void TestPerformance__HeavyMethod__ShouldAllocateNothing()
    // {
    //     var logger = new AccumulationLogger();
    //
    //     var config = ManualConfig.Create(DefaultConfig.Instance)
    //         .AddLogger(logger)
    //         .WithOptions(ConfigOptions.DisableOptimizationsValidator);
    //
    //     BenchmarkRunner.Run<HeavyBenchmarks>(config);
    //
    //     // write benchmark summary
    //     _output.WriteLine(logger.GetLog());
    // }

    // public class HeavyBenchmarks
    // {
    //     [Benchmark]
    //     public void GetSingleMessageContract()
    //     {
    //         var kafkaTopicId = KafkaTopicId.New();
    //         var messageContractId = MessageContractId.New();
    //         var descriptor = Link.GetMethodName<KafkaTopicController>(controller => controller.GetSingleMessageContract(kafkaTopicId, messageContractId));
    //     }
    //
    //     [Benchmark]
    //     public void ProcessRequest()
    //     {
    //         var kafkaTopicId = KafkaTopicId.New();
    //         var messageContractId = MessageContractId.New();
    //         var descriptor = new ActionMethodDescriptor(nameof(KafkaTopicController).Replace("Controller", ""), nameof(KafkaTopicController.GetSingleMessageContract), new RouteValueDictionary
    //         {
    //             ["id"] = kafkaTopicId.ToString(),
    //             ["contractId"] = messageContractId.ToString()
    //         });
    //     }
    // }

    private class FakeLinkGenerator : LinkGenerator
    {
        public override string? GetPathByAddress<TAddress>(HttpContext httpContext, TAddress address, RouteValueDictionary values, RouteValueDictionary? ambientValues = null, PathString? pathBase = null, FragmentString fragment = new FragmentString(), LinkOptions? options = null)
        {
            throw new NotImplementedException();
        }

        public override string? GetPathByAddress<TAddress>(TAddress address, RouteValueDictionary values, PathString pathBase = new PathString(), FragmentString fragment = new FragmentString(), LinkOptions? options = null)
        {
            throw new NotImplementedException();
        }

        public override string? GetUriByAddress<TAddress>(
            HttpContext httpContext,
            TAddress address,
            RouteValueDictionary values,
            RouteValueDictionary? ambientValues = null,
            string? scheme = null,
            HostString? host = null,
            PathString? pathBase = null,
            FragmentString fragment = new(),
            LinkOptions? options = null)
        {
            return string.Join("&", values.Select(x => x.Key + "=" + x.Value));
        }

        public override string? GetUriByAddress<TAddress>(TAddress address, RouteValueDictionary values, string? scheme, HostString host, PathString pathBase = new PathString(), FragmentString fragment = new FragmentString(), LinkOptions? options = null)
        {
            throw new NotImplementedException();
        }
    }
}


public class Link
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LinkGenerator _linkGenerator;

    public Link(IHttpContextAccessor httpContextAccessor, LinkGenerator linkGenerator)
    {
        _httpContextAccessor = httpContextAccessor;
        _linkGenerator = linkGenerator;
    }

    public string? To<T>(Expression<Func<T, object>> method) where T : ControllerBase
    {
        var (controller, action, routeValueDictionary) = GetMethodName(method);

        var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No HttpContext available");

        return _linkGenerator.GetUriByAction(
            httpContext: httpContext,
            controller: controller,
            action: action,
            values: routeValueDictionary
        );
    }

    private record ActionMethodDescriptor(string Controller, string Action, RouteValueDictionary Values);

    private static ActionMethodDescriptor GetMethodName<T>(Expression<Func<T, object>> method)
    {
        if (method is not LambdaExpression lambda)
        {
            throw new ArgumentNullException(nameof(method));
        }

        var methodExpr = lambda.Body.NodeType switch
        {
            ExpressionType.Convert => ((UnaryExpression)lambda.Body).Operand as MethodCallExpression,
            ExpressionType.Call => lambda.Body as MethodCallExpression,
            _ => null
        };

        if (methodExpr == null)
        {
            throw new ArgumentException("Not a member method", nameof(method));
        }

        var values = new RouteValueDictionary();

        var parameters = methodExpr.Method.GetParameters();
        
        for (var i = 0; i < methodExpr.Arguments.Count; i++)
        {
            if (parameters[i].GetCustomAttributesData().Any(y => y.AttributeType == typeof(FromServicesAttribute)))
            {
                continue;
            }
            
            var argument = methodExpr.Arguments[i];
            var name = parameters[i].Name;
            
            var value = GetArgumentValue(argument);

            values.Add(name, value);
        }

        var controller = methodExpr.Method.DeclaringType?.Name.Replace("Controller", "") ?? "";

        return new ActionMethodDescriptor(controller, methodExpr.Method.Name, values);
    }

    private static object? GetArgumentValue(Expression element)
    {
        if (element is ConstantExpression expression)
        {
            return expression.Value;
        }
        var l = Expression.Lambda(Expression.Convert(element, element.Type));
        return l.Compile().DynamicInvoke();
    }
}

public class RunnableInDebugOnlyAttribute : FactAttribute
{
    public RunnableInDebugOnlyAttribute()
    {
        if (!Debugger.IsAttached)
        {
            Skip = "Only running in interactive mode.";
        }
    }
}