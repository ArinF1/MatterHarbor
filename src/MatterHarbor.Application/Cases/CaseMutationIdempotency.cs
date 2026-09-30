using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MatterHarbor.Application.Abstractions;
using MatterHarbor.Domain.Cases;

namespace MatterHarbor.Application.Cases;

internal static class CaseMutationIdempotency
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static string StoreKey(string operation, UserContext user, Guid caseId, string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 200)
        {
            throw new DomainValidationException("Idempotency-Key must contain between 1 and 200 characters.");
        }

        return Hash($"{operation}\n{user.UserId:N}\n{caseId:N}\n{key}");
    }

    public static string RequestHash<T>(T command) => Hash(JsonSerializer.Serialize(command, SerializerOptions));

    public static async Task<CaseResponse?> FindReplayAsync(
        ICaseStore store,
        Guid organizationId,
        string storeKey,
        string requestHash,
        CancellationToken cancellationToken)
    {
        var existing = await store.FindIdempotencyAsync(organizationId, storeKey, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
        {
            throw new IdempotencyConflictException();
        }

        return JsonSerializer.Deserialize<CaseResponse>(existing.ResponseJson, SerializerOptions)
            ?? throw new InvalidOperationException("The stored idempotency response is invalid.");
    }

    public static IdempotencyData Record(
        Guid organizationId,
        string storeKey,
        string requestHash,
        CaseResponse response,
        DateTimeOffset now) => new(
            organizationId,
            storeKey,
            requestHash,
            JsonSerializer.Serialize(response, SerializerOptions),
            now);

    private static string Hash(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}
