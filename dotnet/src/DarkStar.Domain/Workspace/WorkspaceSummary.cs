namespace DarkStar.Domain.Workspace;

public sealed record WorkspaceSummary(
    int KeyCount,
    int ContactCount,
    int AuditCount
);
