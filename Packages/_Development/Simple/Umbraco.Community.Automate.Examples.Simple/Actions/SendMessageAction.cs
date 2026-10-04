using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.Examples.Simple.Configuration;
using Umbraco.Community.Automate.Examples.Simple.Connections;

namespace Umbraco.Community.Automate.Examples.Simple.Actions;

/// <summary>Posts a message to the service using the connection's API key.</summary>
[Action("community.examples.simple.sendMessage", "Send Message (Simple Example)",
    Description = "Posts a message to httpbin.org.",
    ConnectionTypeAlias = "community.examples.simple",
    Group = "Examples",
    Icon = "icon-paper-plane")]
public sealed class SendMessageAction(ActionInfrastructure infrastructure, IHttpClientFactory httpClientFactory)
    : ActionBase<SendMessageSettings, SendMessageOutput>(infrastructure)
{
    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        // 1. Check the inputs first and fail fast with a message the user can act on.
        var apiKey = context.Connection?.GetSettings<SimpleConnectionSettings>().ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            return ActionResult.Failed(new InvalidOperationException("The connection has no API key."), StepRunErrorCategory.ConfigurationError);

        var settings = context.GetSettings<SendMessageSettings>();
        if (string.IsNullOrWhiteSpace(settings.Message))
            return ActionResult.Failed(new InvalidOperationException("A message is required."), StepRunErrorCategory.Validation);

        // 2. Call the service.
        using var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(SimpleConfiguration.BaseUrl), "anything"))
        {
            Content = JsonContent.Create(new { message = settings.Message }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return ActionResult.Failed(ex, StepRunErrorCategory.ServiceUnavailable);
        }

        // 3. Report the result. The category decides whether Automate retries the step:
        //    rate limits and outages are worth retrying, a rejected key isn't.
        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                // httpbin.org echoes the request back; a real service would return what it created.
                var echoed = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
                return Success(new SendMessageOutput
                {
                    Message = echoed.TryGetProperty("json", out var json) && json.TryGetProperty("message", out var message)
                        ? message.GetString() ?? string.Empty
                        : string.Empty,
                    Url = echoed.TryGetProperty("url", out var url) ? url.GetString() ?? string.Empty : string.Empty,
                    ReceivedAt = response.Headers.Date,
                });
            }

            var category = (int)response.StatusCode switch
            {
                401 or 403 => StepRunErrorCategory.Authentication,
                429 => StepRunErrorCategory.RateLimiting,
                >= 500 => StepRunErrorCategory.ServiceUnavailable,
                _ => StepRunErrorCategory.InvalidResponse,
            };
            return ActionResult.Failed(new HttpRequestException($"httpbin.org returned {(int)response.StatusCode}."), category);
        }
    }
}
