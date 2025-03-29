namespace Chat.Api.Messages;

public sealed class ChatMessage : IMessage
{
    public ChatMessage(string text)
    {
        Text = text;
    }

    public string Text { get; }
}
