using System.Globalization;
using System.Net;
using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.WeatherApi.Api;
using Xunit;

namespace Umbraco.Community.Automate.WeatherApi.Tests.Api;

public class WeatherApiRequestHelperTests
{
    [Theory]
    [InlineData("en-GB", "en")]
    [InlineData("fr-FR", "fr")]
    [InlineData("zh-CN", "zh")]
    [InlineData("zh-TW", "zh_tw")]
    [InlineData("zh-Hant-HK", "zh_tw")]
    public void Culture_maps_to_the_weatherapi_language_code(string culture, string expected)
        => Assert.Equal(expected, WeatherApiRequestHelper.ResolveLanguageCode(CultureInfo.GetCultureInfo(culture)));

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, StepRunErrorCategory.Authentication)]
    [InlineData(HttpStatusCode.Forbidden, StepRunErrorCategory.Authentication)]
    [InlineData(HttpStatusCode.BadRequest, StepRunErrorCategory.Validation)]
    [InlineData(HttpStatusCode.TooManyRequests, StepRunErrorCategory.RateLimiting)]
    [InlineData(HttpStatusCode.RequestTimeout, StepRunErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.BadGateway, StepRunErrorCategory.ServiceUnavailable)]
    [InlineData(HttpStatusCode.Conflict, StepRunErrorCategory.InvalidResponse)]
    public void Status_codes_map_to_retry_categories(HttpStatusCode status, StepRunErrorCategory expected)
        => Assert.Equal(expected, WeatherApiRequestHelper.Classify(status));
}
