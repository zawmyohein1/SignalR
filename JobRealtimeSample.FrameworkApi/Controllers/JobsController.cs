using System.Net;
using System.Web.Http;
using JobRealtimeSample.FrameworkApi.Models;
using JobRealtimeSample.FrameworkApi.Services;

namespace JobRealtimeSample.FrameworkApi.Controllers
{
    [RoutePrefix("api/jobs")]
    public sealed class JobsController : ApiController
    {
        private static readonly JobService JobService = new JobService();
        private static readonly RealtimeNotifier RealtimeNotifier = new RealtimeNotifier();
        private static readonly BackgroundJobRunner BackgroundJobRunner = new BackgroundJobRunner(JobService, RealtimeNotifier);

        [HttpPost]
        [Route("start")]
        public IHttpActionResult Start()
        {
            JobInfo job = JobService.CreateJob();

            // The timeout-prone version would do all heavy work before returning.
            // This returns Accepted now and pushes later status updates through SignalR.
            BackgroundJobRunner.RunInBackground(job.JobId);

            return Content(HttpStatusCode.Accepted, new StartJobResponse
            {
                JobId = job.JobId,
                Status = "Accepted",
                Message = "Heavy task started in background"
            });
        }

        [HttpGet]
        [Route("{jobId}")]
        public IHttpActionResult GetById(string jobId)
        {
            JobInfo job = JobService.GetJob(jobId);

            if (job == null)
            {
                return Content(HttpStatusCode.NotFound, new { message = $"Job '{jobId}' was not found." });
            }

            return Ok(job);
        }
    }
}
