namespace JobRealtimeSample.Api.Models;

public sealed record StartJobResponse(
    string JobId,
    string Status,
    string Message);
