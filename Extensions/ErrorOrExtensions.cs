using ErrorOr;

namespace OrbitalDocking.Extensions;

public static class ErrorOrExtensions
{
    public static string ToStatusMessage<T>(this ErrorOr<T> result)
    {
        if (result.IsError)
        {
            return result.FirstError.Description;
        }
        return string.Empty;
    }
}
