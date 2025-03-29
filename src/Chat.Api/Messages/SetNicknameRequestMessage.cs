namespace Chat.Api.Messages;

public sealed class SetNicknameRequestMessage : IMessage
{
    public SetNicknameRequestMessage(Guid requestId, string nickname)
    {
        RequestId = requestId;
        Nickname = nickname;
    }

    public Guid RequestId { get; }
    public string Nickname { get; }
}
