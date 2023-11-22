using System.Net;
using Moq;
using Moq.Protected;

namespace SelfService.Tests;

public abstract class HttpHelper
{
    public static HttpClient CreateMockHttpClient(string mockResponse)
    {
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(mockResponse)
        };

        mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(response);

        var httpClient = new HttpClient(mockHttpHandler.Object);
        httpClient.BaseAddress = new Uri("http://localhost:9090");

        return httpClient;
    }
}
