namespace Umbraco.Community.Automate.Skoda.Configuration;

public class SkodaOptions
{
    public const string SectionName = "Umbraco:Community:Automate:Skoda";

    public Uri BaseUrl { get; set; } = new("https://public.api.connect.skoda-auto.cz/");
}
