using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Moq;
using Moq.Protected;
using Shouldly;
using Umbraco.Community.Automate.Skoda.Configuration;
using Umbraco.Community.Automate.Skoda;
using Xunit;
using Umbraco.Community.Automate.Skoda.Api;
using Umbraco.Community.Automate.Skoda.Models;

namespace Umbraco.Community.Automate.Skoda.Tests.Api;

public class SkodaClientTests
{
    private const string ApiKey = "test-api-key";
    private const string Vin = "TMBJB9NY5RF999999";

    [Fact]
    public async Task GetVehicleAsync_sends_get_request_with_api_key_header_and_returns_parsed_vehicle()
    {
        CapturedRequest? captured = null;
        var sut = CreateSut(
            HttpStatusCode.OK,
            """{"vehicle":{"vin":"TMBJB9NY5RF999999","name":"My Car","licensePlate":"AB123CD","renderUrl":"https://example.com"},"errors":[]}""",
            req => captured = req);

        var result = await sut.GetVehicleAsync(ApiKey, Vin, CancellationToken.None);

        captured!.Method.ShouldBe(HttpMethod.Get);
        captured.RequestUri.ToString().ShouldBe($"{SkodaConfiguration.BaseUrl}api/v1/vehicles/{Vin}");
        captured.Headers.GetValues("X-API-Key").ShouldContain(ApiKey);
        result.Vehicle.Vin.ShouldBe(Vin);
        result.Vehicle.Name.ShouldBe("My Car");
    }

    [Fact]
    public async Task GetVehicleAsync_escapes_special_characters_in_vin()
    {
        CapturedRequest? captured = null;
        var sut = CreateSut(
            HttpStatusCode.OK,
            """{"vehicle":{"vin":"AB/CD EF","name":"My Car","licensePlate":"AB123CD","renderUrl":"https://example.com"},"errors":[]}""",
            req => captured = req);

        await sut.GetVehicleAsync(ApiKey, "AB/CD EF", CancellationToken.None);

        // Uri.ToString() unescapes characters that aren't structurally significant (like a
        // space) for display, but keeps a reserved character like '/' percent-encoded since
        // decoding it would change how the path is parsed - AbsoluteUri shows the wire form.
        captured!.RequestUri.AbsoluteUri.ShouldBe($"{SkodaConfiguration.BaseUrl}api/v1/vehicles/AB%2FCD%20EF");
    }

    [Fact]
    public async Task GetVehicleAsync_throws_when_response_body_is_empty()
    {
        var sut = CreateSut(HttpStatusCode.OK, "null", _ => { });

        var ex = await Should.ThrowAsync<SkodaClientException>(
            () => sut.GetVehicleAsync(ApiKey, Vin, CancellationToken.None));

        ex.Message.ShouldBe("The Škoda API returned an empty vehicle response.");
    }

    [Fact]
    public async Task GetVehicleAsync_throws_SkodaClientException_with_problem_detail_message_on_error()
    {
        var sut = CreateSut(
            HttpStatusCode.NotFound,
            """{"type":"about:blank","title":"Not Found","status":404,"detail":"Vehicle not found","instance":"/api/v1/vehicles/x"}""",
            _ => { });

        var ex = await Should.ThrowAsync<SkodaClientException>(
            () => sut.GetVehicleAsync(ApiKey, Vin, CancellationToken.None));

        ex.Message.ShouldBe("Vehicle not found");
        ex.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ex.Problem!.Title.ShouldBe("Not Found");
    }

    [Fact]
    public async Task GetVehicleAsync_throws_SkodaClientException_with_generic_message_when_body_is_not_json()
    {
        var sut = CreateSut(HttpStatusCode.InternalServerError, "Internal Server Error", _ => { });

        var ex = await Should.ThrowAsync<SkodaClientException>(
            () => sut.GetVehicleAsync(ApiKey, Vin, CancellationToken.None));

        ex.Message.ShouldBe("Škoda API request failed with status code 500.");
        ex.Response.ShouldBe("Internal Server Error");
    }

    [Fact]
    public async Task GetVehicleAsync_surfaces_problem_detail_even_when_the_response_stream_can_only_be_read_once()
    {
        // Regression test: EnsureSuccessAsync used to read the response content twice (once via
        // ReadFromJsonAsync, once via ReadAsStringAsync). That's harmless against the buffered
        // StringContent the other tests use, but a real network response is backed by a
        // forward-only stream - reading it twice silently returns empty content the second time.
        var content = new SingleReadHttpContent(
            """{"type":"about:blank","title":"Not Found","status":404,"detail":"Vehicle not found","instance":"/x"}""");
        var handler = StubHandlerWithContent(HttpStatusCode.NotFound, content);
        var sut = new SkodaClient(CreateHttpClient(handler));

        var ex = await Should.ThrowAsync<SkodaClientException>(
            () => sut.GetVehicleAsync(ApiKey, Vin, CancellationToken.None));

        ex.Message.ShouldBe("Vehicle not found");
        ex.Response!.ShouldContain("Vehicle not found");
    }

    [Fact]
    public async Task StartChargingAsync_sends_post_with_empty_json_body()
    {
        CapturedRequest? captured = null;
        var sut = CreateSut(HttpStatusCode.Accepted, "", req => captured = req);

        await sut.StartChargingAsync(ApiKey, Vin, CancellationToken.None);

        captured!.Method.ShouldBe(HttpMethod.Post);
        captured.RequestUri.ToString().ShouldBe($"{SkodaConfiguration.BaseUrl}api/v1/vehicles/{Vin}/charging/start");
        captured.Body.ShouldBe("{}");
    }

    [Fact]
    public async Task StopChargingAsync_sends_post_to_charging_stop()
    {
        CapturedRequest? captured = null;
        var sut = CreateSut(HttpStatusCode.Accepted, "", req => captured = req);

        await sut.StopChargingAsync(ApiKey, Vin, CancellationToken.None);

        captured!.Method.ShouldBe(HttpMethod.Post);
        captured.RequestUri.ToString().ShouldBe($"{SkodaConfiguration.BaseUrl}api/v1/vehicles/{Vin}/charging/stop");
    }

    [Fact]
    public async Task SetChargingLimitAsync_sends_put_with_target_state_of_charge_body()
    {
        CapturedRequest? captured = null;
        var sut = CreateSut(HttpStatusCode.Accepted, "", req => captured = req);

        await sut.SetChargingLimitAsync(ApiKey, Vin, 80, CancellationToken.None);

        captured!.Method.ShouldBe(HttpMethod.Put);
        captured.RequestUri.ToString().ShouldBe($"{SkodaConfiguration.BaseUrl}api/v1/vehicles/{Vin}/charging/limit");
        captured.Body.ShouldBe("""{"targetStateOfChargeInPercent":80}""");
    }

    [Fact]
    public async Task SetChargeModeAsync_sends_put_with_charge_mode_body()
    {
        CapturedRequest? captured = null;
        var sut = CreateSut(HttpStatusCode.Accepted, "", req => captured = req);

        await sut.SetChargeModeAsync(ApiKey, Vin, "TIMER", CancellationToken.None);

        captured!.Method.ShouldBe(HttpMethod.Put);
        captured.RequestUri.ToString().ShouldBe($"{SkodaConfiguration.BaseUrl}api/v1/vehicles/{Vin}/charging/mode");
        captured.Body.ShouldBe("""{"chargeMode":"TIMER"}""");
    }

    [Fact]
    public async Task UpdateChargingProfileAsync_sends_put_to_charging_profiles_id_with_profile_body()
    {
        CapturedRequest? captured = null;
        var sut = CreateSut(HttpStatusCode.Accepted, "", req => captured = req);

        var profile = new ChargingProfile(
            123456,
            "Home",
            new ChargingProfileSettings("MAXIMUM", new MinBatteryStateOfCharge(true, 20), 80, "PERMANENT"),
            [],
            []);

        await sut.UpdateChargingProfileAsync(ApiKey, Vin, 123456, profile, CancellationToken.None);

        captured!.Method.ShouldBe(HttpMethod.Put);
        captured.RequestUri.ToString().ShouldBe($"{SkodaConfiguration.BaseUrl}api/v1/vehicles/{Vin}/charging-profiles/123456");
        captured.Body!.ShouldContain("\"name\":\"Home\"");
        captured.Body!.ShouldContain("\"targetStateOfChargeInPercent\":80");
    }

    [Fact]
    public async Task StartAirConditioningAsync_sends_post_with_configuration_body()
    {
        CapturedRequest? captured = null;
        var sut = CreateSut(HttpStatusCode.Accepted, "", req => captured = req);

        await sut.StartAirConditioningAsync(
            ApiKey,
            Vin,
            new StartAirConditioningConfiguration(new TargetTemperature(21.5, "CELSIUS"), true),
            CancellationToken.None);

        captured!.Method.ShouldBe(HttpMethod.Post);
        captured.RequestUri.ToString().ShouldBe($"{SkodaConfiguration.BaseUrl}api/v1/vehicles/{Vin}/air-conditioning/start");
        captured.Body.ShouldBe("""{"targetTemperature":{"value":21.5,"unit":"CELSIUS"},"airConditioningWithoutExternalPower":true}""");
    }

    [Fact]
    public async Task StopAirConditioningAsync_sends_post_to_air_conditioning_stop()
    {
        CapturedRequest? captured = null;
        var sut = CreateSut(HttpStatusCode.Accepted, "", req => captured = req);

        await sut.StopAirConditioningAsync(ApiKey, Vin, CancellationToken.None);

        captured!.RequestUri.ToString().ShouldBe($"{SkodaConfiguration.BaseUrl}api/v1/vehicles/{Vin}/air-conditioning/stop");
    }

    [Fact]
    public async Task StartAuxiliaryHeatingAsync_sends_post_with_configuration_body()
    {
        CapturedRequest? captured = null;
        var sut = CreateSut(HttpStatusCode.Accepted, "", req => captured = req);

        await sut.StartAuxiliaryHeatingAsync(
            ApiKey,
            Vin,
            new StartAuxiliaryHeatingConfiguration(new TargetTemperature(21, "CELSIUS"), "1234", 1800, "HEATING"),
            CancellationToken.None);

        captured!.RequestUri.ToString().ShouldBe($"{SkodaConfiguration.BaseUrl}api/v1/vehicles/{Vin}/auxiliary-heating/start");
        captured.Body.ShouldBe("""{"targetTemperature":{"value":21,"unit":"CELSIUS"},"spin":"1234","durationInSeconds":1800,"startMode":"HEATING"}""");
    }

    [Fact]
    public async Task StopAuxiliaryHeatingAsync_sends_post_to_auxiliary_heating_stop()
    {
        CapturedRequest? captured = null;
        var sut = CreateSut(HttpStatusCode.Accepted, "", req => captured = req);

        await sut.StopAuxiliaryHeatingAsync(ApiKey, Vin, CancellationToken.None);

        captured!.RequestUri.ToString().ShouldBe($"{SkodaConfiguration.BaseUrl}api/v1/vehicles/{Vin}/auxiliary-heating/stop");
    }

    [Fact]
    public async Task StartActiveVentilationAsync_sends_post_to_active_ventilation_start()
    {
        CapturedRequest? captured = null;
        var sut = CreateSut(HttpStatusCode.Accepted, "", req => captured = req);

        await sut.StartActiveVentilationAsync(ApiKey, Vin, CancellationToken.None);

        captured!.RequestUri.ToString().ShouldBe($"{SkodaConfiguration.BaseUrl}api/v1/vehicles/{Vin}/active-ventilation/start");
    }

    [Fact]
    public async Task StopActiveVentilationAsync_sends_post_to_active_ventilation_stop()
    {
        CapturedRequest? captured = null;
        var sut = CreateSut(HttpStatusCode.Accepted, "", req => captured = req);

        await sut.StopActiveVentilationAsync(ApiKey, Vin, CancellationToken.None);

        captured!.RequestUri.ToString().ShouldBe($"{SkodaConfiguration.BaseUrl}api/v1/vehicles/{Vin}/active-ventilation/stop");
    }

    private static SkodaClient CreateSut(HttpStatusCode statusCode, string json, Action<CapturedRequest> capture) =>
        new(CreateHttpClient(StubHandler(statusCode, json, capture)));

    private static HttpClient CreateHttpClient(HttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri(SkodaConfiguration.BaseUrl) };

    private static HttpMessageHandler StubHandler(HttpStatusCode code, string json, Action<CapturedRequest> capture) =>
        StubHandlerWithContent(code, new StringContent(json, Encoding.UTF8, "application/json"), capture);

    private static HttpMessageHandler StubHandlerWithContent(
        HttpStatusCode code,
        HttpContent content,
        Action<CapturedRequest>? capture = null)
    {
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                if (capture is not null)
                {
                    var body = req.Content is null ? null : await req.Content.ReadAsStringAsync();
                    capture(new CapturedRequest(req.Method, req.RequestUri!, req.Headers, body));
                }

                return new HttpResponseMessage(code) { Content = content };
            });
        return mock.Object;
    }

    private sealed record CapturedRequest(HttpMethod Method, Uri RequestUri, HttpRequestHeaders Headers, string? Body);

    /// <summary>
    /// Writes its content the first time it's serialized and nothing on subsequent attempts -
    /// simulates a real network response's forward-only stream, which is already drained after
    /// one read.
    /// </summary>
    private sealed class SingleReadHttpContent : HttpContent
    {
        private readonly byte[] _bytes;
        private bool _consumed;

        public SingleReadHttpContent(string content)
        {
            _bytes = Encoding.UTF8.GetBytes(content);
            Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            if (_consumed)
                return;

            _consumed = true;
            await stream.WriteAsync(_bytes);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _bytes.Length;
            return true;
        }
    }
}
