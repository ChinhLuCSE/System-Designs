using NotificationSystem.Shared.Configuration;
using NotificationSystem.Shared.Models;

namespace NotificationSystem.Shared.Services;

public static class TopicNameResolver
{
    public static IEnumerable<string> AllTopics(KafkaTopicOptions topics)
    {
        yield return topics.HighPriority;
        yield return topics.MediumPriority;
        yield return topics.LowPriority;
        yield return topics.Email;
        yield return topics.Sms;
        yield return topics.Push;
        yield return topics.Audit;
        yield return topics.DlqEmail;
        yield return topics.DlqSms;
        yield return topics.DlqPush;
    }

    public static string ResolvePriorityTopic(NotificationPriority priority, KafkaTopicOptions topics) =>
        priority switch
        {
            NotificationPriority.High => topics.HighPriority,
            NotificationPriority.Medium => topics.MediumPriority,
            _ => topics.LowPriority
        };

    public static string ResolveChannelTopic(NotificationChannel channel, KafkaTopicOptions topics) =>
        channel switch
        {
            NotificationChannel.Email => topics.Email,
            NotificationChannel.Sms => topics.Sms,
            _ => topics.Push
        };

    public static string ResolveDlqTopic(NotificationChannel channel, KafkaTopicOptions topics) =>
        channel switch
        {
            NotificationChannel.Email => topics.DlqEmail,
            NotificationChannel.Sms => topics.DlqSms,
            _ => topics.DlqPush
        };
}
