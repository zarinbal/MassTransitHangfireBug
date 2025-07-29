namespace TES.TaskScheduler.Service.Components.Test.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MassTransit;
    using MassTransit.Contracts.JobService;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using TES.TaskScheduler.Service.Components.Test.Consumers;
    using TES.TaskScheduler.Service.Components.Test.Objects;

    [ApiController]
    [Route("[controller]")]
    public class ConvertVideoController :
        ControllerBase
    {
        readonly ILogger<ConvertVideoController> _logger;

        public ConvertVideoController(ILogger<ConvertVideoController> logger)
        {
            _logger = logger;
        }

        [HttpPost("{path}")]
        public async Task<IActionResult> SubmitJob(string path, [FromServices] IRequestClient<SubmitJob<ConvertVideo>> client)
        {
            _logger.LogInformation("Sending job: {Path}", path);

            var groupId = NewId.Next().ToString();
            var convertion = new ConvertVideo
            {
                Path = path,
                GroupId = groupId,
                Index = 0,
                Count = 1,
                Details =
                [
                    new() { Value = "first" },
                    new() { Value = "second" }
                ]
            };

            var jobId = await client.SubmitJob(convertion, new CancellationToken());

            return Ok(new
            {
                jobId,
                path
            });


        }

        [HttpPut("{path}")]
        public async Task<IActionResult> FireAndForgetSubmitJob(string path, [FromServices] IPublishEndpoint publishEndpoint)
        {
            _logger.LogInformation("Sending job: {Path}", path);

            var groupId = NewId.Next().ToString();

            _logger.LogInformation("Scheduling job at (UTC): {Utc}, (Local): {Local}",
    DateTime.UtcNow.AddSeconds(10),
    TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.AddSeconds(10), TimeZoneInfo.Local));

            var scheduledTime = DateTime.UtcNow.AddSeconds(10);

            var jobId = await publishEndpoint.ScheduleJob(scheduledTime, new ConvertVideo
            {
                Path = path,
                GroupId = groupId,
                Index = 0,
                Count = 1,
                Details =
                [
                    new() { Value = "first" },
                    new() { Value = "second" }
                ]
            });


            return Ok(new
            {
                result = jobId,
                Path = path
            });
        }

        [HttpPost("{count:int}")]
        public async Task<IActionResult> SubmitJob(int count, [FromServices] IRequestClient<ConvertVideo> client)
        {
            var jobIds = new List<Guid>(count);

            var groupId = NewId.Next().ToString();

            for (var i = 0; i < count; i++)
            {
                var path = NewId.Next() + ".txt";

                var jobId = await client.SubmitJob(new ConvertVideo
                {
                    Path = path,
                    GroupId = groupId,
                    Index = i,
                    Count = count,
                });

                jobIds.Add(jobId);
            }

            return Ok(new { jobIds });
        }

        [HttpGet("{jobId:guid}")]
        public async Task<IActionResult> GetJobState(Guid jobId, [FromServices] IRequestClient<GetJobState> client)
        {
            try
            {
                var jobState = await client.GetJobState(jobId);

                return Ok(new
                {
                    jobId,
                    jobState.CurrentState,
                    jobState.Submitted,
                    jobState.Started,
                    jobState.Completed,
                    jobState.Faulted,
                    jobState.Reason,
                    jobState.LastRetryAttempt,
                });
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpDelete("{jobId:guid}")]
        public async Task<IActionResult> CancelJob(Guid jobId, [FromServices] IPublishEndpoint publishEndpoint)
        {
            await publishEndpoint.CancelJob(jobId, "User Request");

            return Ok();
        }

        [HttpPost("{jobId:guid}")]
        public async Task<IActionResult> RetryJob(Guid jobId, [FromServices] IPublishEndpoint publishEndpoint)
        {
            await publishEndpoint.RetryJob(jobId);

            return Ok();
        }
    }
}