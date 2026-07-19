namespace DarkStar.Api.Contracts;

public sealed record SignTextApiRequest(
    string Message,
    string Secret
);
