using System.Text.Json;
using MatterHarbor.Application.Abstractions;
using MatterHarbor.Domain.Auditing;
using MatterHarbor.Domain.Cases;

namespace MatterHarbor.Application.Cases;

public sealed class CreateCaseService(ICaseStore store, IClock clock)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<CreateCaseResult> ExecuteAsync(
        UserContext user,
        string idempotencyKey,
        CreateCaseCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 200)
        {
            throw new DomainValidationException("Idempotency-Key must contain between 1 and 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.Title) || command.Title.Trim().Length > 200)
        {
            throw new DomainValidationException("title must contain between 1 and 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.Description) || command.Description.Trim().Length > 4_000)
        {
            throw new DomainValidationException("description must contain between 1 and 4000 characters.");
        }

        if (!Enum.IsDefined(command.Priority))
        {
            throw new DomainValidationException("Priority is invalid.");
        }

        var requestHash = IdempotencyHasher.Hash(command);
        await using var transaction = await store.BeginTransactionAsync(cancellationToken);
        await store.AcquireIdempotencyLockAsync(user.OrganizationId, idempotencyKey, cancellationToken);

        var existing = await store.FindIdempotencyAsync(user.OrganizationId, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
            {
                throw new IdempotencyConflictException();
            }

            var original = JsonSerializer.Deserialize<CaseResponse>(existing.ResponseJson, SerializerOptions)
                ?? throw new InvalidOperationException("The stored idempotency response is invalid.");
            await transaction.CommitAsync(cancellationToken);
            return new CreateCaseResult(original, true);
        }

        if (command.AssignedUserId is { } assignedUserId &&
            !await store.UserBelongsToOrganizationAsync(user.OrganizationId, assignedUserId, cancellationToken))
        {
            throw new AssignedUserNotFoundException();
        }

        var now = clock.UtcNow;
        var caseId = Guid.NewGuid();
        var caseNumber = $"OC-{now:yyyyMMdd}-{caseId.ToString("N")[..8].ToUpperInvariant()}";
        var caseItem = CaseItem.Create(
            user.OrganizationId,
            caseNumber,
            command.Title,
            command.Description,
            command.Priority,
            command.AssignedUserId,
            now,
            caseId);
        var response = CaseResponse.From(caseItem);

        store.AddCase(caseItem);
        store.AddAudit(new AuditEntry(Guid.NewGuid(), user.OrganizationId, user.UserId, caseId, "case.created", now));
        store.AddOutbox(new OutboxData(
            Guid.NewGuid(),
            user.OrganizationId,
            "case.created",
            JsonSerializer.Serialize(new { CaseId = caseId, user.OrganizationId }, SerializerOptions),
            now));
        store.AddIdempotency(new IdempotencyData(
            user.OrganizationId,
            idempotencyKey,
            requestHash,
            JsonSerializer.Serialize(response, SerializerOptions),
            now));

        await store.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new CreateCaseResult(response, false);
    }
}
