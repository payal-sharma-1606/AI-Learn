namespace SmartNotes.Api.Exceptions;

/// <summary>
/// Raised when an AI result could not be produced. <see cref="IsTransient"/> distinguishes
/// "try again in a moment" (rate limits, timeouts) from "this will keep failing".
/// </summary>
public class AiServiceException : Exception
{
    public AiServiceException(string message, bool isTransient, Exception? innerException = null)
        : base(message, innerException)
    {
        IsTransient = isTransient;
    }

    public bool IsTransient { get; }
}
