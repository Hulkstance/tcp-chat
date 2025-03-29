using System.Net;
using System.Net.Sockets;
using Chat.Api;
using Chat.Api.Messages;
using Chat.Server;

Console.WriteLine("Starting server...");

var connections = new ConnectionCollection();
var cancellationTokenSource = new CancellationTokenSource();

var listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
listeningSocket.Bind(new IPEndPoint(IPAddress.Any, 33333));
listeningSocket.Listen();

Console.WriteLine("Listening...");

Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cancellationTokenSource.Cancel();
    Console.WriteLine("Shutting down server...");
};

try
{
    while (!cancellationTokenSource.Token.IsCancellationRequested)
    {
        var connectedSocket = await listeningSocket.AcceptAsync(cancellationTokenSource.Token);
        Console.WriteLine($"Got a connection from {connectedSocket.RemoteEndPoint} to {connectedSocket.LocalEndPoint}.");

        // Start connection handling as a proper Task (not async void)
        _ = ProcessSocketMainLoopAsync(connectedSocket, cancellationTokenSource.Token)
            .ContinueWith(t => {
                if (t.IsFaulted)
                {
                    Console.WriteLine($"Unhandled exception in socket processing: {t.Exception}");
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Accept operation was canceled.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error in main listening loop: {ex.Message}");
}
finally
{
    listeningSocket.Close();
    Console.WriteLine("Server shut down.");
}

async Task ProcessSocketMainLoopAsync(Socket socket, CancellationToken cancellationToken)
{
    var chatConnection = new ChatConnection(new PipelineSocket(socket));
    var clientConnection = connections.Add(chatConnection);

    try
    {
        await foreach (var message in chatConnection.InputMessages.WithCancellation(cancellationToken))
        {
            if (message is ChatMessage chatMessage)
            {
                Console.WriteLine($"Got message from {chatConnection.RemoteEndPoint}: {chatMessage.Text}");

                var currentConnections = connections.CurrentConnections;
                var from = clientConnection.Nickname ?? chatConnection.RemoteEndPoint.ToString();
                var broadcastMessage = new BroadcastMessage(from, chatMessage.Text);
                var tasks = currentConnections
                    .Select(x => x.ChatConnection)
                    .Where(x => x != chatConnection)
                    .Select(connection => connection.SendMessageAsync(broadcastMessage));

                try
                {
                    await Task.WhenAll(tasks);
                }
                catch
                {
                    // ignored
                }
            }
            else if (message is SetNicknameRequestMessage setNicknameRequestMessage)
            {
                Console.WriteLine($"Got nickname request message from {chatConnection.RemoteEndPoint}: {setNicknameRequestMessage.Nickname}");

                if (connections.TrySetNickname(chatConnection, setNicknameRequestMessage.Nickname))
                {
                    await chatConnection.SendMessageAsync(new AckResponseMessage(setNicknameRequestMessage.RequestId));
                }
                else
                {
                    await chatConnection.SendMessageAsync(new NakResponseMessage(setNicknameRequestMessage.RequestId, "Nickname already taken."));
                }
            }
            else
                Console.WriteLine($"Got unknown message from {chatConnection.RemoteEndPoint}.");
        }

        Console.WriteLine($"Connection at {chatConnection.RemoteEndPoint} was disconnected.");
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine($"Processing for {chatConnection.RemoteEndPoint} was canceled.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception from {chatConnection.RemoteEndPoint}: [{ex.GetType().Name}] {ex.Message}");
    }
    finally
    {
        connections.Remove(chatConnection);
        
        try
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }
        catch
        {
            // ignored
        }
    }
}
