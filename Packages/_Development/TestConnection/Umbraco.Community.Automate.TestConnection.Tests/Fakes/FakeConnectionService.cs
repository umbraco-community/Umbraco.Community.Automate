using Umbraco.Automate.Core.Connections;

namespace Umbraco.Community.Automate.TestConnection.Tests.Fakes;

/// <summary>
/// Knows one connection (or none) and answers its Test connection check with a canned result.
/// Only the two calls the action makes are implemented.
/// </summary>
public sealed class FakeConnectionService(Connection? connection, ConnectionValidationResult? testResult) : IConnectionService
{
    public List<Guid> Tested { get; } = [];

    public Task<Connection?> GetConnectionByAliasAsync(string alias, CancellationToken cancellationToken = default)
        => Task.FromResult(connection?.Alias == alias ? connection : null);

    public Task<ConnectionValidationResult?> TestConnectionAsync(Guid connectionId, CancellationToken cancellationToken = default)
    {
        Tested.Add(connectionId);
        return Task.FromResult(testResult);
    }

    public Task<Connection?> GetConnectionAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<Connection>> GetAllConnectionsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<(IEnumerable<Connection>, int)> GetConnectionsPagedAsync(string? filter, int skip, int take, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<Connection> CreateConnectionAsync(Connection connection, Guid? userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<Connection> UpdateConnectionAsync(Connection connection, Guid? userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<bool> DeleteConnectionAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<ConfiguredConnection?> GetConfiguredConnectionAsync(Guid connectionId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IReadOnlyList<ConfiguredConnection>> GetConfiguredConnectionsByIdsAsync(IReadOnlyCollection<Guid> connectionIds, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<Connection> RollbackConnectionAsync(Guid connectionId, int targetVersion, Guid? userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
