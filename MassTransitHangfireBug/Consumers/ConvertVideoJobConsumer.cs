using MassTransit;
using MassTransitHangfireBug.Objects;

namespace MassTransitHangfireBug.Consumers;
public class ConvertVideoJobConsumer :
        IJobConsumer<ConvertVideo>
{
    readonly ILogger<ConvertVideoJobConsumer> _logger;

    public ConvertVideoJobConsumer(ILogger<ConvertVideoJobConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Run(JobContext<ConvertVideo> context)
    {
        var variance = context.TryGetJobState(out ConsumerState? state)
            ? TimeSpan.FromMilliseconds(state!.Variance)
            : TimeSpan.FromMilliseconds(Random.Shared.Next(8399, 28377));

        _logger.LogInformation("Converting Video: {GroupId} {Path}", context.Job.GroupId, context.Job.Path);
        var starttime = DateTime.Now;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await context.SetJobProgress(0, (long)variance.TotalMilliseconds);

            for (var i = 0; i < 5; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), context.CancellationToken);
            }

            _logger.LogInformation("Converted Video: {GroupId} {Path}", context.Job.GroupId, context.Job.Path);
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            await context.SaveJobState(new ConsumerState { Variance = (long)variance.TotalMilliseconds });
            
            //throw;
        }
    }
}


class ConsumerState
{
    public long Variance { get; set; }
}
