using Microsoft.Extensions.Logging;
using System.Net;
using SelfService.Infrastructure.Api.Prometheus;
using SelfService.Application;
using Moq;
using Moq.Protected;

namespace SelfService.Tests.Infrastructure.Api;

public class TestPrometheusClientResultParsing
{
    private HttpClient getMockedHttpClient(string mockResponse)
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

    [Fact]
    public async Task parsing_failure_produces_empty_list()
    {
        // create dummy input
        string mockResponse =
            @"{
            ""status"": ""failure""
        }";

        var loggerMock = new Mock<ILogger<PrometheusClient>>();
        var httpClient = getMockedHttpClient(mockResponse);

        IKafkaTopicConsumerService service = new PrometheusClient(loggerMock.Object, httpClient);

        // define expectations
        var expected = new List<string>();

        // apply parser
        try
        {
            var actual = await service.GetConsumersForKafkaTopic("topic");
            Assert.Equal(expected, actual);
        }
        catch (Exception e)
        {
            Assert.Fail("Unexpected exception was thrown: " + e.Message);
        }
    }

    [Fact]
    public async Task parsing_success_with_incomplete_data_produces_empty_list()
    {
        // create dummy input
        List<string> mockResponses = new List<string>();
        mockResponses.Append(
            @"{
            ""status"": ""success""
        }"
        );
        mockResponses.Append(
            @"{
            ""status"": ""success"",
            ""data"": {
                ""resultType"": ""vector""
            }
        }"
        );
        mockResponses.Append(
            @"{
            ""status"": ""success"",
            ""data"": {
                ""resultType"": ""vector"",
                ""result"": [
                    {
                        ""value"": [
                            123,
                            ""456""
                        ]
                    },
            }
        }"
        );
        mockResponses.Append(
            @"{
            ""status"": ""success"",
            ""data"": {
                ""resultType"": ""vector"",
                ""result"": [
                    {
                        ""metric"": {
                            ""partition"": ""0""
                        },
                        ""value"": [
                            123,
                            ""456""
                        ]
                    },
            }
        }"
        );

        // define expectations
        var expected = new List<string>();

        foreach (string mockResponse in mockResponses)
        {
            var loggerMock = new Mock<ILogger<PrometheusClient>>();
            var httpClient = getMockedHttpClient(mockResponse);

            IKafkaTopicConsumerService service = new PrometheusClient(loggerMock.Object, httpClient);

            try
            {
                var actual = await service.GetConsumersForKafkaTopic("doesnot matter");
                Assert.Equal(expected, actual);
            }
            catch (Exception e)
            {
                Assert.Fail("Unexpected exception was thrown: " + e.Message);
            }
        }
    }

    [Fact]
    public async Task parsing_success_with_legal_result_returns_all_consumers_for_topic_across_partitions()
    {
        // create dummy input
        string mockResponse =
            @"{
            ""status"": ""success"",
            ""data"": {
                ""resultType"": ""vector"",
                ""result"": [
                    {
                        ""metric"": {
                            ""consumergroup"": ""consumer1"",
                            ""topic"": ""topic"",
                            ""partition"": ""0""
                        },
                        ""value"": [
                            123,
                            ""456""
                        ]
                    },
                    {
                        ""metric"": {
                            ""consumergroup"": ""consumer2"",
                            ""topic"": ""topic"",
                            ""partition"": ""1""
                        },
                        ""value"": [
                            123,
                            ""456""
                        ]
                    },
                    {
                        ""metric"": {
                            ""consumergroup"": ""consumer3"",
                            ""topic"": ""topic2"",
                            ""partition"": ""0""
                        },
                        ""value"": [
                            123,
                            ""456""
                        ]
                    }
                ] 
            }
        }";

        var loggerMock = new Mock<ILogger<PrometheusClient>>();
        var httpClient = getMockedHttpClient(mockResponse);

        IKafkaTopicConsumerService service = new PrometheusClient(loggerMock.Object, httpClient);

        // verification
        var expected = new List<string> { "consumer1", "consumer2" };
        try
        {
            var actual = await service.GetConsumersForKafkaTopic("topic");
            Assert.Equal(expected, actual);
        }
        catch (Exception e)
        {
            Assert.Fail("Unexpected exception was thrown: " + e.Message);
        }

        var expected2 = new List<string> { "consumer3" };
        try
        {
            var actual2 = await service.GetConsumersForKafkaTopic("topic2");
            Assert.Equal(expected2, actual2);
        }
        catch (Exception e)
        {
            Assert.Fail("Unexpected exception was thrown: " + e.Message);
        }
    }
}
