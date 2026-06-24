using Shouldly;
using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.GoogleSheets.Actions;
using Xunit;

namespace Umbraco.Community.Automate.GoogleSheets.Tests.Actions;

public class GoogleApiErrorParserTests
{
    [Fact]
    public void Parse_maps_permission_denied_to_a_friendly_message_and_invalid_response()
    {
        var (message, category) = GoogleApiErrorParser.Parse(403,
            """{"error":{"code":403,"message":"The caller does not have permission","status":"PERMISSION_DENIED"}}""");

        category.ShouldBe(StepRunErrorCategory.InvalidResponse);
        message.ShouldContain("doesn't have access to that spreadsheet");
    }

    [Fact]
    public void Parse_maps_not_found_to_a_friendly_message_and_validation()
    {
        var (message, category) = GoogleApiErrorParser.Parse(404,
            """{"error":{"code":404,"message":"Requested entity was not found.","status":"NOT_FOUND"}}""");

        category.ShouldBe(StepRunErrorCategory.Validation);
        message.ShouldContain("couldn't find a spreadsheet");
    }

    [Fact]
    public void Parse_maps_invalid_argument_to_a_friendly_message_and_validation()
    {
        var (message, category) = GoogleApiErrorParser.Parse(400,
            """{"error":{"code":400,"message":"Invalid range.","status":"INVALID_ARGUMENT"}}""");

        category.ShouldBe(StepRunErrorCategory.Validation);
        message.ShouldContain("rejected the spreadsheet ID or sheet name");
    }

    [Fact]
    public void Parse_falls_back_to_raw_body_for_unrecognized_status()
    {
        var (message, category) = GoogleApiErrorParser.Parse(500,
            """{"error":{"code":500,"message":"Internal error.","status":"INTERNAL"}}""");

        category.ShouldBe(StepRunErrorCategory.InvalidResponse);
        message.ShouldContain("500");
        message.ShouldContain("INTERNAL");
    }

    [Fact]
    public void Parse_falls_back_to_raw_body_when_not_json()
    {
        var (message, category) = GoogleApiErrorParser.Parse(503, "<html>Service Unavailable</html>");

        category.ShouldBe(StepRunErrorCategory.InvalidResponse);
        message.ShouldContain("503");
        message.ShouldContain("<html>Service Unavailable</html>");
    }

    [Fact]
    public void Parse_falls_back_to_raw_body_when_error_object_missing()
    {
        var (message, category) = GoogleApiErrorParser.Parse(500, "{}");

        category.ShouldBe(StepRunErrorCategory.InvalidResponse);
        message.ShouldContain("500");
    }
}
