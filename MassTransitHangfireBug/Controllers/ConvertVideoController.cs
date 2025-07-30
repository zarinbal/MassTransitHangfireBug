namespace MassTransitHangfireBug.Controllers
{
    using MassTransit;
    using MassTransit.Contracts.JobService;
    using MassTransit.JobService.Messages;
    using MassTransitHangfireBug.Objects;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading;
    using System.Threading.Tasks;


    [ApiController]
    [Route("[controller]")]
    public class ConvertVideoController :
        ControllerBase
    {
        readonly IMessageScheduler _scheduler;
        readonly ILogger<ConvertVideoController> _logger;
        readonly IPublishEndpoint _publishEndpoint;
        private readonly IRequestClient<GetJobState> _jobStateClient;

        public ConvertVideoController(ILogger<ConvertVideoController> logger, IMessageScheduler scheduler, IPublishEndpoint publishEndpoint,

            IRequestClient<GetJobState> client
            )
        {
            _logger = logger;
            _scheduler = scheduler;
            _publishEndpoint = publishEndpoint;
            _jobStateClient = client;
        }

        [HttpPut("immediate/{path?}")]
        public async Task<IActionResult> SubmitJob(string? path, IRequestClient<SubmitJob<ConvertVideo>> client)
        {
            _logger.LogInformation("Sending Immediate job");
            var conversion = GetConversionObject(path);
            var jobId = await client.SubmitJob(conversion, new CancellationToken());
            return Ok(new
            {
                JobId = jobId,
                Video = conversion
            });
        }

        private async Task<IActionResult> ScheduleJob(string? path, DateTime scheduledTime)
        {
            var conversion = GetConversionObject(path, scheduledTime);
            var jobId = await _publishEndpoint.ScheduleJob(scheduledTime, conversion);

            _logger.LogInformation("Scheduling job at (UTC): {Utc}, (Local): {Local}",
                       scheduledTime.ToUniversalTime(),
                       scheduledTime.ToLocalTime());

            Thread.Sleep(1000); // Simulate some delay for the job to be processed

            var status = await _jobStateClient.GetJobState(jobId);

            return Ok(new
            {
                JobId = jobId,
                Video = conversion,
                Status = status
            });
        }

        private async Task<IActionResult> ScheduleJobModified(string? path, DateTimeOffset scheduledTime)
        {
            var conversion = GetConversionObject(path, scheduledTime);
            
            _logger.LogInformation("Scheduling job at Datetime Offset (UTC): {Utc}, (Local): {Local}",
                       scheduledTime.ToUniversalTime(),
                       scheduledTime.ToLocalTime());

            //var jobId = await _publishEndpoint.ScheduleJob(scheduledTime, conversion);
            var jobId = NewId.NextGuid();
            await _publishEndpoint.Publish<SubmitJob<ConvertVideo>>(new SubmitJobCommand<ConvertVideo>
            {
                JobId = jobId,
                Job = conversion,
                Schedule = new RecurringJobScheduleInfo { Start = scheduledTime },

            }, new CancellationToken()).ConfigureAwait(false);

            Thread.Sleep(1000); // Simulate some delay for the job to be processed
            var status = await _jobStateClient.GetJobState(jobId);
            return Ok(new
            {
                JobId = jobId,
                Video = conversion,
                Status = status
            });
        }

        [HttpPut("job/modified/utc/{path?}")]
        public async Task<IActionResult> FireAndForgetSubmitJobModified(string? path)
        {
            _logger.LogInformation("Sending modified Local job: {Path}", path);

            var scheduledTime = DateTimeOffset.UtcNow.AddSeconds(10);

            var response = await ScheduleJobModified(path, scheduledTime);
            return response;
        }

        [HttpPut("job/modified/local/{path?}")]
        public async Task<IActionResult> FireAndForgetSubmitJobLocalModified(string? path)
        {
            _logger.LogInformation("Sending modified Local job: {Path}", path);

            var scheduledTime = DateTimeOffset.Now.AddSeconds(10);

            var response = await ScheduleJobModified(path, scheduledTime);
            return response;
        }

        [HttpPut("job/utc/{path?}")]
        public async Task<IActionResult> FireAndForgetSubmitJob(string? path)
        {
            _logger.LogInformation("Sending Local job: {Path}", path);

            var scheduledTime = DateTime.UtcNow.AddSeconds(10);
           

            var response = await ScheduleJob(path, scheduledTime);
            return response;
        }

        [HttpPut("job/local/{path?}")]
        public async Task<IActionResult> FireAndForgetSubmitJobLocal(string? path)
        {
            _logger.LogInformation("Sending Local job: {Path}", path);

            var scheduledTime = DateTime.Now.AddSeconds(10);

            _logger.LogInformation("Scheduling job at (UTC): {Utc}, (Local): {Local}",
                                scheduledTime.ToLocalTime(),
                                   scheduledTime);

            var response = await ScheduleJob(path, scheduledTime);
            return response;
        }

        [HttpPut("publish/utc/{path?}")]
        public async Task<IActionResult> SchedulePublishUtcTime(string? path)
        {
            _logger.LogInformation("Schedule publish message: {Path}", path);

            var scheduledTime = DateTime.UtcNow.AddSeconds(10);

            _logger.LogInformation("Scheduling publish message at (UTC): {Utc}, (Local): {Local}",
                                   scheduledTime,
                                   scheduledTime.ToLocalTime());

            var response = await SchedulePublish(path, scheduledTime);
            return response;
        }


        [HttpPut("publish/local/{path?}")]
        public async Task<IActionResult> SchedulePublishLocalTime(string? path)
        {
            _logger.LogInformation("Schedule publish message: {Path}", path);

            var scheduledTime = DateTime.Now.AddSeconds(10);

            _logger.LogInformation("Scheduling job at (UTC): {Utc}, (Local): {Local}",
                                scheduledTime.ToLocalTime(),
                                   scheduledTime);

            var response = await SchedulePublish(path, scheduledTime);
            return response;
        }


        // This endpoint is used to schedule a publish message for a video conversion
        private async Task<IActionResult> SchedulePublish(string? path, DateTime scheduledTime)
        {
            var conversion = GetConversionObject(path, scheduledTime);
            var result = await _scheduler.SchedulePublish<ConvertVideo>(scheduledTime, conversion);
            return Ok(new
            {
                Result = result,
                Video = conversion
            });
        }

        [HttpGet("State/{jobId:guid}")]
        public async Task<IActionResult> GetJobState(Guid jobId)
        {
            try
            {
                var jobState = await _jobStateClient.GetJobState(jobId);

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
        public async Task<IActionResult> CancelJob(Guid jobId)
        {
            await _publishEndpoint.CancelJob(jobId, "User Request");

            return Ok();
        }

        [HttpPost("{jobId:guid}")]
        public async Task<IActionResult> RetryJob(Guid jobId)
        {
            await _publishEndpoint.RetryJob(jobId);

            return Ok();
        }

        private ConvertVideo GetConversionObject(string? path, DateTimeOffset? scheduledTime = null)
        {
            var groupId = NewId.Next().ToString();
            return new ConvertVideo
            {
                Path = path,
                GroupId = groupId,
                Index = 0,
                Count = 1,
                ScheduledTime = scheduledTime,
                Details =
                [
                    new() { Value = scheduledTime?.ToString() ?? "Immediate" }
                ]
            };
        }
    }
}