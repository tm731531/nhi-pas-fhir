using System;
using System.Net.Http;
using Hl7.Fhir.Model;
using NhiPasFhir.Core;
using Xunit;
using Task = System.Threading.Tasks.Task;

// DryRunPasSubmitter: validates + reports the would-send payload, transmits nothing.
public class DryRunSubmitterTests
{
    [Fact]
    public async Task Dry_run_validates_and_reports_but_does_not_transmit()
    {
        var bundle = new Bundle
        {
            Type = Bundle.BundleType.Collection,
            Entry = { new Bundle.EntryComponent { FullUrl = "Patient/p1", Resource = new Patient { Id = "p1" } } },
        };

        var result = await DryRunPasSubmitter.Instance.SubmitAsync(bundle);

        Assert.True(DryRunPasSubmitter.Instance.Enabled);
        Assert.False(result.Accepted);          // nothing transmitted
        Assert.Equal("dry-run", result.Status);
        Assert.Null(result.Response);           // no receiver response
        Assert.Contains("DRY-RUN", result.Message);
        Assert.Contains("bytes", result.Message);
    }

    [Fact]
    public void Select_picks_the_submitter_by_mode()
    {
        Assert.IsType<NoSubmitter>(Submitters.Select(SubmitterMode.None));
        Assert.IsType<DryRunPasSubmitter>(Submitters.Select(SubmitterMode.DryRun));
        Assert.IsType<HttpPasSubmitter>(Submitters.Select(SubmitterMode.Http, new HttpClient(), "https://example.test/pas"));
    }

    [Fact]
    public void Select_http_without_client_or_endpoint_fails_loud()
    {
        Assert.Throws<ArgumentNullException>(() => Submitters.Select(SubmitterMode.Http));
        Assert.Throws<ArgumentNullException>(() => Submitters.Select(SubmitterMode.Http, new HttpClient()));
    }
}
