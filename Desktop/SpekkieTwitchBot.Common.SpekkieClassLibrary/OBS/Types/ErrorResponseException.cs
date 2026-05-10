namespace SpekkieClassLibrary.OBS.Types;

public class AuthFailureException : Exception
{
}

public class ErrorResponseException : Exception
{
    public ErrorResponseException(string message, int errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    public int ErrorCode { get; set; }
}