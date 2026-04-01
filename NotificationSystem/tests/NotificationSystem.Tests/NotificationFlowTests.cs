using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NotificationSystem.Shared.Abstractions;
using NotificationSystem.Shared.Configuration;
using NotificationSystem.Shared.Models;
using NotificationSystem.Shared.Services;

namespace NotificationSystem.Tests;

public sealed class NotificationFlowTests
{
    [Fact]
    public async Task Processor_Should_Cancel_When_No_Active_Device_Setting()
    {
        var repository = new FakeNotificationRepository();
        var publisher = new FakeMessagePublisher();
        repository.Notifications[SampleEnvelope.NotificationId] = SampleRecord with { Status = NotificationStatus.Queued };
        repository.ActiveDeviceSetting = null;

        var service = new NotificationProcessingService(
            repository,
            new FakeRateLimiter(true),
            new FakeBlueprintCache(),
            publisher,
            Options.Create(CreateInfrastructureOptions()),
            TimeProvider.System);

        await service.ProcessAsync(SampleEnvelope, CancellationToken.None);

        repository.LastStatus.Should().Be(NotificationStatus.Cancelled);
        publisher.Published.Should().ContainSingle(message => message.Topic == CreateInfrastructureOptions().Kafka.Topics.Audit);
    }

    [Fact]
    public async Task Processor_Should_Publish_To_Channel_Topic_When_Allowed()
    {
        var repository = new FakeNotificationRepository();
        var publisher = new FakeMessagePublisher();
        repository.Notifications[SampleEnvelope.NotificationId] = SampleRecord with { Status = NotificationStatus.Queued };
        repository.ActiveDeviceSetting = SampleDeviceSetting;
        repository.Blueprint = SampleBlueprint;

        var service = new NotificationProcessingService(
            repository,
            new FakeRateLimiter(true),
            new FakeBlueprintCache(),
            publisher,
            Options.Create(CreateInfrastructureOptions()),
            TimeProvider.System);

        await service.ProcessAsync(SampleEnvelope with { BlueprintId = SampleBlueprint.Id }, CancellationToken.None);

        repository.LastStatus.Should().Be(NotificationStatus.Processing);
        publisher.Published.Should().Contain(message => message.Topic == CreateInfrastructureOptions().Kafka.Topics.Email);
    }

    [Fact]
    public async Task ChannelDelivery_Should_Requeue_On_Transient_Failure_Before_Max_Retries()
    {
        var repository = new FakeNotificationRepository { ActiveDeviceSetting = SampleDeviceSetting };
        var publisher = new FakeMessagePublisher();
        var service = new ChannelDeliveryService(
            [new FakeEmailNotificationProvider()],
            repository,
            new FakeBlueprintCache(),
            publisher,
            Options.Create(CreateInfrastructureOptions(baseDelaySeconds: 0, maxRetries: 3)),
            NullLogger<ChannelDeliveryService>.Instance,
            TimeProvider.System);

        await service.DeliverAsync(SampleEnvelope with { Metadata = new Dictionary<string, string> { ["mode"] = "transient-fail" } }, CancellationToken.None);

        repository.LastStatus.Should().Be(NotificationStatus.Failed);
        repository.LastAttemptCount.Should().Be(1);
        publisher.Published.Should().Contain(message => message.Topic == CreateInfrastructureOptions().Kafka.Topics.Email);
        publisher.Published.Should().Contain(message => message.Topic == CreateInfrastructureOptions().Kafka.Topics.Audit);
    }

    [Fact]
    public async Task ChannelDelivery_Should_Move_To_Dlq_On_Permanent_Failure()
    {
        var repository = new FakeNotificationRepository { ActiveDeviceSetting = SampleDeviceSetting };
        var publisher = new FakeMessagePublisher();
        var service = new ChannelDeliveryService(
            [new FakeEmailNotificationProvider()],
            repository,
            new FakeBlueprintCache(),
            publisher,
            Options.Create(CreateInfrastructureOptions(baseDelaySeconds: 0, maxRetries: 3)),
            NullLogger<ChannelDeliveryService>.Instance,
            TimeProvider.System);

        await service.DeliverAsync(SampleEnvelope with { Metadata = new Dictionary<string, string> { ["mode"] = "permanent-fail" } }, CancellationToken.None);

        repository.LastStatus.Should().Be(NotificationStatus.DeadLettered);
        publisher.Published.Should().Contain(message => message.Topic == CreateInfrastructureOptions().Kafka.Topics.DlqEmail);
    }

    private static InfrastructureOptions CreateInfrastructureOptions(int baseDelaySeconds = 0, int maxRetries = 3) => new()
    {
        Delivery = new DeliveryOptions
        {
            BaseDelaySeconds = baseDelaySeconds,
            MaxRetries = maxRetries
        }
    };

    private static readonly NotificationEnvelope SampleEnvelope = new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        "user-1",
        NotificationChannel.Email,
        NotificationPriority.High,
        null,
        new Dictionary<string, string> { ["message"] = "hello" },
        0,
        "corr-1",
        DateTimeOffset.UtcNow,
        "orders-service",
        new Dictionary<string, string>());

    private static readonly NotificationRecord SampleRecord = new(
        SampleEnvelope.NotificationId,
        SampleEnvelope.UserId,
        SampleEnvelope.Channel,
        SampleEnvelope.Priority,
        SampleEnvelope.BlueprintId,
        "{}",
        SampleEnvelope.SourceService,
        null,
        SampleEnvelope.CorrelationId,
        NotificationStatus.Pending,
        0,
        null,
        DateTimeOffset.UtcNow,
        DateTimeOffset.UtcNow,
        null);

    private static readonly DeviceSetting SampleDeviceSetting = new(1, "user-1", NotificationChannel.Email, "user-1@example.com", null, true);

    private static readonly NotificationBlueprint SampleBlueprint = new("welcome-email", NotificationChannel.Email, "subject", "content", true);

    private sealed class FakeNotificationRepository : INotificationRepository
    {
        public Dictionary<Guid, NotificationRecord> Notifications { get; } = [];

        public DeviceSetting? ActiveDeviceSetting { get; set; }

        public NotificationBlueprint? Blueprint { get; set; }

        public NotificationStatus? LastStatus { get; private set; }

        public int? LastAttemptCount { get; private set; }

        public Task CreateAsync(NotificationRecord notification, CancellationToken cancellationToken)
        {
            Notifications[notification.Id] = notification;
            return Task.CompletedTask;
        }

        public Task EnsureSchemaAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<NotificationRecord?> GetByIdAsync(Guid notificationId, CancellationToken cancellationToken)
            => Task.FromResult(Notifications.TryGetValue(notificationId, out var record) ? record : null);

        public Task<DeviceSetting?> GetActiveDeviceSettingAsync(string userId, NotificationChannel channel, CancellationToken cancellationToken)
            => Task.FromResult(ActiveDeviceSetting);

        public Task<NotificationBlueprint?> GetBlueprintAsync(string blueprintId, CancellationToken cancellationToken)
            => Task.FromResult(Blueprint);

        public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateStatusAsync(Guid notificationId, NotificationStatus status, int? attemptCount, string? lastError, DateTimeOffset? sentAt, CancellationToken cancellationToken)
        {
            LastStatus = status;
            LastAttemptCount = attemptCount;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeBlueprintCache : IBlueprintCache
    {
        public NotificationBlueprint? Blueprint { get; private set; }

        public Task<NotificationBlueprint?> GetAsync(string blueprintId, CancellationToken cancellationToken)
            => Task.FromResult(Blueprint);

        public Task SetAsync(NotificationBlueprint blueprint, CancellationToken cancellationToken)
        {
            Blueprint = blueprint;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRateLimiter(bool allowed) : IRateLimiter
    {
        public Task<RateLimitResult> EvaluateAsync(NotificationEnvelope envelope, CancellationToken cancellationToken)
            => Task.FromResult(new RateLimitResult(allowed, 1, 2, DateTimeOffset.UtcNow.AddHours(24)));
    }

    private sealed class FakeMessagePublisher : IMessagePublisher
    {
        public List<(string Topic, string Key, object Payload)> Published { get; } = [];

        public Task PublishAsync<T>(string topic, string key, T payload, CancellationToken cancellationToken)
        {
            Published.Add((topic, key, payload!));
            return Task.CompletedTask;
        }
    }
}
