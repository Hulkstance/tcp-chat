using Chat.Api;

namespace Chat.Server;

public sealed class ClientChatConnection
{
    public ClientChatConnection(ChatConnection chatConnection)
    {
        ChatConnection = chatConnection;
    }

    public ChatConnection ChatConnection { get; }

    public string? Nickname { get; set; }
}
