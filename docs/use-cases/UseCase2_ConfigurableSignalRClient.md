# Use Case 2 - Configurable SignalR Client In MVC UI

## Requirement

SignalR client-side connection in the MVC UI must be configurable.

The existing system should not be affected by adding SignalR support.

Not every page should create a SignalR connection.

## Main Concern

Existing application has many pages and modules.

Some pages need realtime job status.
Some pages do not need SignalR.

Therefore, SignalR should be opt-in.

## Desired Behavior

```text
Page A uses SignalR
  -> SignalR connection is created

Page B does not use SignalR
  -> SignalR connection is not created

Page C supports SignalR but config disables it
  -> SignalR connection is not created
```

## Recommended Design

Default behavior:

```text
SignalR disabled unless explicitly enabled
```

Configuration values:

```text
SignalR.Enabled = true / false
SignalR.HubUrl = https://localhost:5003/hubs/jobstatus
```

Page-level behavior:

```text
Only pages that need realtime job status render SignalR config and scripts.
```

JavaScript behavior:

```javascript
if (!window.jobSignalRConfig || !window.jobSignalRConfig.enabled) {
    return;
}
```

## Suggested MVC UI Flow

```text
MVC Controller / View
    |
    | Read Web.config SignalR settings
    v
Razor View
    |
    | Render config only if page needs SignalR
    v
JavaScript
    |
    | If enabled, create SignalR connection
    | If disabled, do nothing
    v
SignalR Hub
```

## Example Configuration

```xml
<appSettings>
  <add key="JobSignalR.Enabled" value="true" />
  <add key="JobSignalR.HubUrl" value="https://localhost:5003/hubs/jobstatus" />
</appSettings>
```

## Example Page Config Object

```html
<script>
  window.jobSignalRConfig = {
      enabled: true,
      hubUrl: "https://localhost:5003/hubs/jobstatus",
      apiBaseUrl: "http://localhost:5002"
  };
</script>
```

## Example JavaScript Guard

```javascript
(function () {
    if (!window.jobSignalRConfig || !window.jobSignalRConfig.enabled) {
        return;
    }

    // Create SignalR connection only when enabled.
})();
```

## Why This Protects Existing System

This approach prevents global SignalR side effects.

Existing pages are not changed unless they explicitly opt in.

No SignalR connection is created on pages that do not need it.

No extra browser connection load is added for unrelated modules.

No old jQuery SignalR dependency is introduced.

## Final Decision

This use case is possible and recommended.

SignalR client integration should be:

```text
configuration-based
page-specific
opt-in
disabled by default
safe for existing pages
```
