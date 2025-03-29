namespace Chat.Api.Messages;

public sealed class BroadcastMessage : IMessage
{
    public BroadcastMessage(string from, string text)
    {
        From = from;
        Text = text;
    }

    public string From { get; }
    public string Text { get; }
}
