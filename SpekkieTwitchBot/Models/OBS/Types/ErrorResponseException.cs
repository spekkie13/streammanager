namespace SpekkieTwitchBot.Models.OBS.Types;

public class AuthFailureException : Exception
{
}

public class ErrorResponseException : Exception
{
    public int ErrorCode { get; set; }

    public ErrorResponseException(string message, int errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }
}