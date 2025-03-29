namespace Chat.Api.Messages;

public sealed class NakResponseMessage : IMessage
{
    public NakResponseMessage(Guid requestId, string message)
    {
        RequestId = requestId;
        Message = message;
    }

    public Guid RequestId { get; }
    public string Message { get; }
}
