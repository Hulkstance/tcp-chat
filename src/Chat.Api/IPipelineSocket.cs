using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

namespace Chat.Api;

public interface IPipelineSocket : IDuplexPipe
{
    Socket Socket { get; }
    
    uint MaxMessageSize { get; }

    IPEndPoint RemoteEndPoint { get; }
}
