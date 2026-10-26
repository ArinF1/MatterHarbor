using System.Globalization;
using MatterHarbor.Domain.Cases;

namespace MatterHarbor.Api.Contracts;

public static class CaseVersionHeaders
{
    public static void SetEtag(HttpResponse response, int version) =>
        response.Headers.ETag = $"\"v{version.ToString(CultureInfo.InvariantCulture)}\"";

    public static int ExpectedVersion(HttpRequest request)
    {
        var ifMatch = request.Headers.IfMatch.ToString();
        if (string.IsNullOrEmpty(ifMatch))
        {
            throw new CasePreconditionRequiredException();
        }

        if (ifMatch.Length < 4 || !ifMatch.StartsWith("\"v", StringComparison.Ordinal) ||
            !ifMatch.EndsWith('"') ||
            !int.TryParse(ifMatch.AsSpan(2, ifMatch.Length - 3), NumberStyles.None,
                CultureInfo.InvariantCulture, out var version) || version < 1)
        {
            throw new DomainValidationException("If-Match must contain one case version ETag.");
        }

        return version;
    }
}

public sealed class CasePreconditionRequiredException()
    : Exception("If-Match with the current case ETag is required.");
