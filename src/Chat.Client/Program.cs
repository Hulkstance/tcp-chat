using System.Net.Sockets;
using Chat.Api;
using Chat.Api.Messages;

ChatConnection? chatConnection = null;
var cts = new CancellationTokenSource();
var isConnected = false;

Console.Title = "Chat Client";
Console.WriteLine("=== Console Chat Client ===");
Console.WriteLine("Available commands:");
Console.WriteLine("/connect - Connect to the chat server");
Console.WriteLine("/nick <nickname> - Set your nickname");
Console.WriteLine("/exit - Exit the chat app");
Console.WriteLine("Any other text will be sent as a chat message once connected");
Console.WriteLine();

_ = Task.Run(ProcessUserInputAsync);

try
{
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (OperationCanceledException)
{
    // ignored
}

if (isConnected)
{
    chatConnection?.Complete();
    Console.WriteLine("Disconnected from server.");
}

async Task ProcessUserInputAsync()
{
    while (!cts.Token.IsCancellationRequested)
    {
        var input = Console.ReadLine();
        
        if (string.IsNullOrEmpty(input))
            continue;

        if (input.StartsWith('/'))
        {
            await ProcessCommandAsync(input);
        }
        else if (isConnected)
        {
            await SendChatMessageAsync(input);
        }
        else
        {
            Console.WriteLine("Not connected. Use /connect to connect to the server first.");
        }
    }
}

async Task ProcessCommandAsync(string command)
{
    var parts = command.Split(' ', 2);
    var cmd = parts[0].ToLower();

    switch (cmd)
    {
        case "/connect":
            await ConnectToServerAsync();
            break;

        case "/nick":
            if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
            {
                Console.WriteLine("Please provide a nickname. Usage: /nick <nickname>");
            }
            else
            {
                await SetNicknameAsync(parts[1]);
            }
            break;

        case "/exit":
            Console.WriteLine("Exiting chat application...");
            cts.Cancel();
            break;

        default:
            Console.WriteLine($"Unknown command: {cmd}");
            break;
    }
}

async Task ConnectToServerAsync()
{
    if (isConnected)
    {
        Console.WriteLine("Already connected to the server.");
        return;
    }

    try
    {
        Console.WriteLine("Connecting to chat server...");
        
        var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await clientSocket.ConnectAsync("localhost", 33333);

        Console.WriteLine($"Connected to {clientSocket.RemoteEndPoint}");

        chatConnection = new ChatConnection(new PipelineSocket(clientSocket));
        isConnected = true;

        // Start processing incoming messages in the background
        _ = ProcessSocketAsync(chatConnection);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to connect: {ex.Message}");
    }
}

async Task SendChatMessageAsync(string message)
{
    if (!isConnected || chatConnection == null)
    {
        Console.WriteLine("Not connected to server.");
        return;
    }

    try
    {
        await chatConnection.SendMessageAsync(new ChatMessage(message));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to send message: {ex.Message}");
    }
}

async Task SetNicknameAsync(string nickname)
{
    if (!isConnected || chatConnection == null)
    {
        Console.WriteLine("Not connected to server.");
        return;
    }

    try
    {
        Console.WriteLine($"Setting nickname to '{nickname}'...");
        await chatConnection.SetNicknameAsync(nickname);
        Console.WriteLine($"Nickname successfully set to '{nickname}'");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to set nickname: {ex.Message}");
    }
}

async Task ProcessSocketAsync(ChatConnection connection)
{
    try
    {
        await foreach (var message in connection.InputMessages)
        {
            if (message is BroadcastMessage broadcastMessage)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{broadcastMessage.From}: ");
                Console.ResetColor();
                Console.WriteLine(broadcastMessage.Text);
            }
            else
            {
                Console.WriteLine($"Received unknown message type from server.");
            }
        }

        Console.WriteLine("Disconnected from server.");
        isConnected = false;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Connection error: [{ex.GetType().Name}] {ex.Message}");
        isConnected = false;
    }
}
