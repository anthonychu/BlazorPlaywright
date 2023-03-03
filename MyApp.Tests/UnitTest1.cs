using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class Tests : PageTest
{
    string _tracePath = "";
    bool _traceEnabled = false;
    public Tests()
    {
        _tracePath = Environment.GetEnvironmentVariable("TRACE_PATH") ?? "";
        _traceEnabled = !string.IsNullOrEmpty(_tracePath);
    }

    [SetUp]
    public async Task Setup()
    {
        var baseUrl = Environment.GetEnvironmentVariable("SITE_BASE_URL") ?? "http://localhost:5215/";
        await Page.GotoAsync(baseUrl);
    }

    [Test]
    public async Task HomePageHasCorrectTitle()
    {
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Hello, world!" })).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 20000
            });
    }

    [Test]
    public async Task CounterWorks()
    {
        await Page.GetByRole(AriaRole.Link, new() { Name = "Counter" }).ClickAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Click me" }).ClickAsync(new LocatorClickOptions
        {
            ClickCount = 5,
        });

        await Expect(Page.Locator("#app > div > main > article > p")).ToHaveTextAsync("Current count: 5");
    }

    [Test]
    public async Task FetchDataWorks()
    {
        if (_traceEnabled)
        {
            await Page.Context.Tracing.StartAsync(new()
            {
                Screenshots = true,
                Snapshots = true,
                Sources = true,
            });
        }

        var jsonText = @"[
            {
            ""date"": ""2022-01-06"",
            ""temperatureC"": 1,
            ""summary"": ""Freezing""
            },
            {
            ""date"": ""2022-01-07"",
            ""temperatureC"": 14,
            ""summary"": ""Bracing""
            },
            {
            ""date"": ""2022-01-08"",
            ""temperatureC"": -13,
            ""summary"": ""Freezing""
            },
            {
            ""date"": ""2022-01-09"",
            ""temperatureC"": -16,
            ""summary"": ""Balmy""
            },
            {
            ""date"": ""2022-01-10"",
            ""temperatureC"": -2,
            ""summary"": ""Chilly""
            }
        ]";

        var json = System.Text.Json.JsonSerializer.Deserialize<dynamic>(jsonText);

        await Page.RouteAsync("https://randomweather.azurewebsites.net/api/weatherforecast", async route =>
        {
            await route.FulfillAsync(new() { Json = json });
        });

        await Page.GetByRole(AriaRole.Link, new() { Name = "Fetch data" }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = "Date" })).ToBeVisibleAsync();
        var rows = await Page.Locator("#app > div > main > article > table > tbody > tr").AllAsync();

        Assert.That(rows.Count, Is.EqualTo(5));

        await Expect(Page.Locator("#app > div > main > article > table > tbody > tr:nth-child(1) > td:nth-child(4)")).ToHaveTextAsync("Freezing");

        if (_traceEnabled)
        {
            await Page.Context.Tracing.StopAsync(new()
            {
                Path = Path.Combine(_tracePath, $"{nameof(FetchDataWorks)}.zip"),
            });
        }
    }

}