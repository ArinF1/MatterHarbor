using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MatterHarbor.Application.Cases;

public static class IdempotencyHasher
{
    public static string Hash(CreateCaseCommand command)
    {
        var canonical = JsonSerializer.Serialize(new
        {
            Title = command.Title.Trim(),
            Description = command.Description.Trim(),
            Priority = (int)command.Priority,
            command.AssignedUserId
        });

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
