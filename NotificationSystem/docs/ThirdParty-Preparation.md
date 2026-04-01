# Third-Party Preparation

## Email

- Select the production email provider and define the fallback provider strategy.
- Verify sending domains and configure `SPF`, `DKIM`, and `DMARC`.
- Decide whether templates stay in the notification service or move to provider-native templates.
- Define secret injection for API keys and outbound webhook validation if delivery callbacks are added later.

## SMS

- Choose provider coverage by target country/region.
- Provision sender numbers or short codes.
- Confirm regulatory requirements for opt-in/opt-out and message content by market.
- Define throttling and spending guards outside the notification service.

## Push

- Prepare `FCM` credentials for Android.
- Prepare `APNs` token/key material for iOS.
- Document bundle IDs / package IDs and environment separation (`dev`, `staging`, `prod`).
- Decide certificate/key rotation flow and secret storage.

## Cross-cutting

- Store provider credentials outside source control.
- Decide whether delivery receipts will update the operational status table later.
- Add provider-specific health checks and richer retry classification in phase 2.
