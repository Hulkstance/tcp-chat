namespace Chat.Api.Messages;

public sealed class AckResponseMessage : IMessage
{
    public AckResponseMessage(Guid requestId)
    {
        RequestId = requestId;
    }

    public Guid RequestId { get; }
}
