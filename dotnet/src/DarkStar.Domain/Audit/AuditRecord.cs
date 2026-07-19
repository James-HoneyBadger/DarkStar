namespace DarkStar.Domain.Audit;

public sealed record AuditRecord(
    DateTimeOffset Timestamp,
    string Operation,
    string Subject,
    string Metadata
);
