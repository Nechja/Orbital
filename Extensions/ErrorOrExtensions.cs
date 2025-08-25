using System;
using System.Diagnostics;
using System.Linq;
using ErrorOr;

namespace OrbitalDocking.Extensions;

public static class ErrorOrExtensions
{
    public static string ToStatusMessage<T>(this ErrorOr<T> result)
    {
        if (result.IsError)
        {
            var firstError = result.FirstError;
            return firstError.Description;
        }
        return string.Empty;
    }
    
    public static void LogErrors<T>(this ErrorOr<T> result, string context = "")
    {
        if (result.IsError)
        {
            foreach (var error in result.Errors)
            {
                Debug.WriteLine($"[{context}] {error.Type}: {error.Code} - {error.Description}");
            }
        }
    }
    
    public static bool IsSuccess<T>(this ErrorOr<T> result) => !result.IsError;
    
    public static T? GetValueOrDefault<T>(this ErrorOr<T> result, T? defaultValue = default)
    {
        return result.IsError ? defaultValue : result.Value;
    }
    
    public static ErrorOr<T> OnSuccess<T>(this ErrorOr<T> result, Action<T> action)
    {
        if (!result.IsError)
        {
            action(result.Value);
        }
        return result;
    }
    
    public static ErrorOr<T> OnError<T>(this ErrorOr<T> result, Action<Error> action)
    {
        if (result.IsError)
        {
            action(result.FirstError);
        }
        return result;
    }
}