using Chat.Api;

namespace Chat.Server;

public sealed class ConnectionCollection
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly List<ClientChatConnection> _connections = [];

    public IReadOnlyCollection<ClientChatConnection> CurrentConnections
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _connections.ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public ClientChatConnection Add(ChatConnection connection)
    {
        _lock.EnterWriteLock();
        try
        {
            var clientConnection = new ClientChatConnection(connection);
            _connections.Add(clientConnection);
            return clientConnection;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Remove(ChatConnection connection)
    {
        _lock.EnterWriteLock();
        try
        {
            _connections.RemoveAll(x => x.ChatConnection == connection);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public bool TrySetNickname(ChatConnection connection, string nickname)
    {
        _lock.EnterUpgradeableReadLock();
        try
        {
            var clientChatConnection = _connections.FirstOrDefault(x => x.ChatConnection == connection);
            if (clientChatConnection == null)
                return false;

            if (clientChatConnection.Nickname == nickname)
                return true;

            if (_connections.Any(x => x.Nickname == nickname))
                return false;

            _lock.EnterWriteLock();
            try
            {
                clientChatConnection.Nickname = nickname;
                return true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }
}
