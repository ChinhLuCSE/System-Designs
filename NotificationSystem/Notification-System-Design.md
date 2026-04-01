# Notification System Design

## Table of Contents

- [Introduction](#introduction)
- [Requirements](#requirements)
- [Data Model](#data-model)
- [Basic Implementation](#basic-implementation)
- [Advanced Implementation](#advanced-implementation)
- [Additional discussion points](#Additional-discussion-points)

## Introduction

- A notification system is a system that provides a means of delivering a message to a set of recipients.

- There are many different channels notifications can be delivered through including Email, SMS, iOS, Android and many others. Nowadays notification systems are commonplace, (e.g. placing and order on Amazon and receiving an email confirming the order has been placed).

## Requirements

- Functional Requirements
+ Send notifications to different channels (Email, SMS, iOS, Android)
+ Extendable (ability to add more notification channels)

- Non Functional Requirements
+ High availability
+ High scalability
+ Reliable (no notifications are lost)

- Not covered
+ Bulk sending of notifications (e.g. for all users that match a specific criteria)

## Data Model

![schema](./images/schema.avif)

This is a basic outline of some of the core tables that could be included in a notification system data model.

- users
+ Contains information related to the user.

- device_settings
+ user_id: Foreign key which is used to identify which user to which the notification was sent.
+ device_token: The token which is used to enable the pushing of notifications to different services (e.g. FCM, APNs)
+ notification_channel: The channel which the settings belong to (e.g. email, SMS etc.)
+ is_active: Whether a user has consented to receiving notifications for that device.

- notifications
+ user_id: Foreign key which is used to determine which user is associated with the notification.
+ blueprint_id: Foreign key which is used to determine which blueprint is associated with the notification.
+ status: pending - Notification created but not yet processed, sent - Successfully delivered to the user, failed - Delivery attempt failed (network issues, invalid recipient, etc.), cancelled - Notification was cancelled before sending

Retry logic - Failed notifications can be identified and retried
Delivery tracking - Know which notifications were successfully delivered
Debugging - Troubleshoot delivery issues by filtering failed notifications
Analytics - Calculate delivery success rates and system performance
Cleanup - Remove old sent/cancelled notifications during data retention
The status works together with sent_at - when status changes to "sent", the sent_at timestamp is populated.

- notification_blueprints
+ content: The actual contents if the notification (e.g. HTML, plain text, JSON etc.)
+ channel: The channel which this blueprint is intended to be sent to (e.g., email, SMS etc.)

To support bulk notifications a table user_notifications could be added to create a many to many relationship between users and notifications.

## Basic Implementation

![basic-implementation](./images/basic-implementation.avif)

- Notification Sources
+ Notification Sources are the upstream services that trigger notifications and can include internal services, external services, scripts, and cron jobs.

- Load Balancer
+ Ensures that requests are distributed evenly among the Notification Handler servers.

- Notification Servers
+ The servers will check the device_settings table to ensure that a user has consented to receive notifications in this channel. Given the relational nature of the data this database could be implemented with an SQL database.
+ Rate limiting can also be implemented here with a cache like Redis for each user to track how many notifications of each type they have been sent. Rules can be configured to improve the user experience (e.g. a user can receive a maximum of only two promotional marketing notifications per day).

- Notification Queues
+ If a user hasn't consented to receiving notifications on that channel, the system can stop processing the notification.
+ If the user has consented, the system can then push the message onto the relevant queue based on the associated channel.
+ This can be implemented with Kafka or Amazon SNS, or Pub/Sub in Google Cloud Platform.
+ In this system we will use Kafka and to prevent duplicates during retries, Kafka offers an idempotent producer feature. When enabled, the producer ensures exactly once delivery to a Kafka topic, even across retries.
+ Notification servers are the producers and each topic corresponds to a specific channel.
+ It is easy to add new topics to Kafka which makes the system extendable for adding channels in the future as the system evolves.

- Lightweight Handlers
+ The lightweight handlers act as the consumers and pull messages off their designated topic. These could be implemented with AWS lambda functions or Google Cloud Platform Functions.
+ Each lightweight handler will query the notification cache and database to get the blueprint associated with the notification. The notification can then be sent onto the relevant third party service.
+ If message processing fails, the lightweight handler can either retry immediately or reset the offset to reprocess the message later. Commit strategies (automatic or manual) and policies (e.g. at least once, at most once, exactly once) influence how and when offsets are committed.
+ Exponential backoff is a strategy used in computing to gradually and adaptively increase the waiting time between retries of a failed operation, with the goal of reducing the load on the system and increasing the likelihood of success in subsequent attempts.
+ A dead-letter queue (DLQ) could be implemented for messages that can't be processed after several attempts, a common pattern is to redirect these messages to a dead-letter queue, a separate Kafka topic can be created where failed messages are stored for further investigation or reprocessing.

- Third Party Services
+ For iOS and Android notifications they can be sent to Apple Push Notification Service (APNs) and Firebase Cloud Messaging (FCM) respectively.
+ For email notifications, using third party services like MailChimp or MailJet is a good option as they handle many common issues when trying to send emails at scale include implementing authentication protocols such as Sender Policy Framework (SPF), DomainKeys Identified Mail (DKIM), and Domain-based Message Authentication, Reporting, and Conformance (DMARC), as well as reputation management and IP warming, which ensure that emails are actually delivered and not marked as spam. They also include other tools like audience segmentation and analytics so the system will be able to see which notifications users are actually viewing.
+ For SMS notifications, using third party services like Twilio is a good option as it again handles the infrastructure to ensure SMSs are actually delivered and will also provide monitoring and analytics to see which SMS notifications are performing well.

## Advanced Implementation

![arcitecture-overview](./images/arcitecture-overview.avif)

The Advanced Implementation is identical to the basic Implementation except for the following additions:

- Notification Handlers
+ This service has a few roles including simple authentication to ensure that the service interacting with the notification service is able to do so. We can use basic API keys to ensure that a service has the appropriate access level.
The handlers also push each message onto the priority queues.

- Priority Queues
+ Each notification belongs to a channel and some channels will be more important than others (e.g. billing and invoice notifications are typically more important than generic marketing notifications)
+ Each message will be placed on a queue and this is done as we want to improve scalability, and to decouple the system services.
+ To implement this you could use something like Kafka or Amazon SNS, or Pub/Sub in Google Cloud Platform.
+ In this system we will use Kafka. While Kafka doesn't natively support priority queues, you can implement a similar mechanism by creating separate topics for each priority level (e.g. high, medium, low). Producers send notifications to the appropriate topic based on their priority. Consumers first consume messages from higher-priority topics before moving to lower-priority ones. This approach is simple and leverages Kafka's scalability and reliability, but it requires careful consumer management to ensure that higher-priority messages are always processed first.

- Notification Servers
+ Pulls messages off the priority queues with a bias towards pulling from the more important queues to ensure that the more important notifications are being handled first. Also implements rate limiting and ensures the user has consented to receive notifications for that channel as described in the Basic Implementation.

- Notification Logger
+ A log queue can be added to the Notification Queues where every message is sent regardless of its channel and then stored in the Notification Database which can be used for auditing purposes. Given that this database will be write heavy with bulk read, a NoSQL database like Cassandra would be a good choice.

- Analytics Service
+ The analytics service will be used to see the lifecycle of each notification as it passes through the system.
Again this database will be write heavy with bulk read, a NoSQL database like Cassandra would be a good choice.

## Additional Discussion Points

- Reliability
+ Multi-data center approach.
+ Kafka can act as a highly durable message buffer, ensuring that messages are not lost even in the event of processing failures.
+ Retry mechanisms: Elaborate on how the system can intelligently retry sending notifications without overwhelming the server or the user, including exponential backoff strategies.
+ Dead letter queues to ensure delivery

- Security and Privacy
+ Address how you would protect user data and ensure that the notification system complies with data protection regulations like GDPR or CCPA.

- Basic vs Advanced Implementation
+ In some interviews there may not be enough time or the interviewer may not want the additional services implemented in the Advanced Implementation like prioritisation, auditing, or analytics, so make sure to ask clarifying questions early to determine which implementation would best suit the interview.