using JobRealtimeSample.Api.Models;
using JobRealtimeSample.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobRealtimeSample.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class JobsController(
    JobService jobService,
    BackgroundJobRunner backgroundJobRunner) : ControllerBase
{
    [HttpPost("start")]
    public ActionResult<StartJobResponse> Start()
    {
        var job = jobService.CreateJob();

        // The timeout-prone version would await the heavy task here before
        // returning a response. This endpoint returns a JobId immediately and
        // lets the background runner report progress through SignalR instead.
        backgroundJobRunner.RunInBackground(job.JobId);

        return Accepted(new StartJobResponse(
            job.JobId,
            "Accepted",
            "Heavy task started in background"));
    }

    [HttpGet("{jobId}")]
    public ActionResult<JobInfo> GetById(string jobId)
    {
        var job = jobService.GetJob(jobId);

        return job is null
            ? NotFound(new { message = $"Job '{jobId}' was not found." })
            : Ok(job);
    }
}
