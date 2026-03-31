# Notification System Design

## Table of Contents

- [Introduction](#introduction)
- [Requirements](#requirements)
- [Data Model](#data-model)
- [API Design](#api-design)
- [Key Questions](#key-questions)
- [Basic Implementation](#basic-implementation)
- [Advanced Implementation](#advanced-implementation)
- [Create URL Flow](#Create-URL-Flow)
- [Redirect URL Flow](#Redirect-URL-Flow)
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