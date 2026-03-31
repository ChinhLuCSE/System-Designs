using org.apache.zookeeper;

namespace TinyURL.Api.Infrastructure;

public sealed class ZooKeeperConnectionWatcher(ILogger<ZooKeeperConnectionWatcher> logger) : Watcher
{
    public override Task process(WatchedEvent @event)
    {
        logger.LogInformation("ZooKeeper event received: state={State}, type={Type}, path={Path}", @event.getState(), @event.get_Type(), @event.getPath());
        return Task.CompletedTask;
    }
}
