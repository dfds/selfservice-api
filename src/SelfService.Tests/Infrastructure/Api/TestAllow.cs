using System.Text.Json;
using SelfService.Infrastructure.Api;

namespace SelfService.Tests.Infrastructure.Api;

public class TestAllow
{
    [Fact]
    public void can_initialize_allowed_methods()
    {
        var allowed = new Allow
        {
            Method.Get, Method.Post, Method.Put, Method.Delete, Method.Patch
        };

        Assert.Equal(new[] { "GET", "POST", "PUT", "DELETE", "PATCH" }, allowed);
    }

    [Fact]
    public void can_add_allowed_methods()
    {
        var a = Allow.Get;
        a += Method.Post;

        Assert.Equal(new[] { "GET", "POST" }, a);
    }

    [Fact]
    public void ignore_duplicate_allowed_methods()
    {
        var a = Allow.Get;
        a += Method.Get;

        Assert.Equal(new[] { "GET" }, a);
    }

    [Fact]
    public void none_allowed_methods_is_empty()
    {
        Assert.Empty(Allow.None);
    }

    [Fact]
    public void none_allowed_methods_serializes_as_empty_json_array()
    {
        var json = JsonSerializer.Serialize(Allow.None, JsonSerializerOptions.Default);

        Assert.Equal("[]", json);
    }

    [Fact]
    public void allowed_methods_serializes_as_json_string_array()
    {
        var json = JsonSerializer.Serialize(new Allow { Method.Get, Method.Post }, JsonSerializerOptions.Default);

        Assert.Equal("[\"GET\",\"POST\"]", json);
    }
}