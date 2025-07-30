namespace MassTransitHangfireBug.Objects
{
    using System;
    using System.Collections.Generic;


    public record ConvertVideo
    {
        public string? GroupId { get; init; }
        public int Index { get; init; }
        public int Count { get; init; }
        public string? Path { get; init; }

        public ICollection<VideoDetail>? Details { get; init; } = new List<VideoDetail>();
        public DateTime? ScheduledTime { get; internal set; }
    }
}