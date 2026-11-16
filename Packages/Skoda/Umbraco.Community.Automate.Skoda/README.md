# Umbraco.Community.Automate.Skoda

A Škoda connection type and actions for [Umbraco Automate](https://github.com/umbraco/Umbraco.Automate), built on the MyŠkoda Public API.

Read a vehicle's status and control charging, air conditioning, auxiliary heating and ventilation as part of an automation workflow.

> **In development.** This package hasn't been released to NuGet yet.

## Installation

```bash
dotnet add package Umbraco.Community.Automate.Skoda
```

There's nothing to register in code: Umbraco loads the package on its own. Then follow **Setup** below.

## Setup

### 1. Get an API key

Generate an API key in the **MyŠkoda** app. You'll also need the vehicle's VIN (Vehicle Identification Number).

### 2. Store the key in configuration

Add the key under `Umbraco:Automate:Secrets:Skoda`, in Umbraco Automate's shared Secrets section (locally, use [user secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)). If you'll use **Start Auxiliary Heating**, add the vehicle's S-PIN there too:

```json
{
  "Umbraco": {
    "Automate": {
      "Secrets": {
        "Skoda": {
          "ApiKey": "your-api-key",
          "Spin": "your-s-pin"
        }
      }
    }
  }
}
```

In production, use environment variables instead: `Umbraco__Automate__Secrets__Skoda__ApiKey=your-api-key` and `Umbraco__Automate__Secrets__Skoda__Spin=your-s-pin`. Umbraco Automate resolves references from this shared section out of the box, so there's nothing to register.

### 3. Create the connection

1. Go to **Automation → Settings → Connections** and create a new **Škoda** connection.
2. **API key** is already filled in with `$Umbraco:Automate:Secrets:Skoda:ApiKey`, a reference to the key you stored in step 2. Leave it, or replace it with the key itself to store it on the connection.
3. **VIN**: the vehicle you want to control. Create one connection per vehicle.
4. **Validate connection**: when on, saving or testing the connection checks the API key and VIN against the Škoda API. Each check uses one request from your API quota, so it's off by default.

## Actions

All actions are in the **Vehicles** group and act on the vehicle set on the connection.

| Action | Settings |
|---|---|
| **Get Vehicle Status** | None. Returns the vehicle's details and current state (see [Outputs](#outputs)). |
| **Start Charging** / **Stop Charging** | None. |
| **Set Charge Mode** | **Charge mode**, chosen from a list: Manual, Timer, Timer with climatisation, Preferred charging times, Only own current, Immediate discharging or Home storage charging. |
| **Set Charging Limit** | **Target state of charge** in percent, from 50 to 100 in steps of 10. |
| **Update Charging Profile** | **Charging profile ID** (the profile's `id` under `chargingProfiles.profiles` in the MyŠkoda Public API's vehicle response, `GET /api/v1/vehicles/{vin}`), plus any of **Name**, **Target state of charge**, **Max charging current (AC)** (Reduced or Maximum) and **Auto unlock plug when charged** (Permanently or Off). Empty fields, and **Keep current setting** in the lists, keep the profile's current value. |
| **Start Air Conditioning** | **Target temperature**, **Temperature unit** (Celsius or Fahrenheit), **Allow without external power**. |
| **Stop Air Conditioning** | None. |
| **Start Auxiliary Heating** | **Target temperature**, **Temperature unit**, **S-PIN** (required; pre-filled with `$Umbraco:Automate:Secrets:Skoda:Spin`, stored encrypted and masked in run logs), **Duration in seconds** (1 to 3,600, default 1,800), **Start mode** (Heating or Ventilation). |
| **Stop Auxiliary Heating** | None. |
| **Start Active Ventilation** / **Stop Active Ventilation** | None. |

Choices such as the charge mode or temperature unit are picked from a list. The charging profile **Name** supports `${ binding }` expressions, so it can come from the trigger or an earlier step.

### Outputs

**Get Vehicle Status** makes these available to later steps as `${ steps.<alias>.<output> }`:

| Output | Description |
|---|---|
| `vin`, `name`, `licensePlate` | Vehicle details |
| `mileageInKm` | Odometer reading |
| `stateOfChargeInPercent`, `remainingCruisingRangeInMeters` | Battery and range |
| `chargingState`, `airConditioningState`, `parkingState` | Current states |
| `latitude`, `longitude`, `formattedAddress` | Parked position |
| `errors` | Any errors the vehicle reported (`type`, `description`) |

The command actions output the `vin` they acted on.

## Troubleshooting

| The step fails with | What to do |
|---|---|
| *The API key reference '...' could not be resolved* | The connection points at a configuration key that isn't there. Add the key (see [step 2](#2-store-the-key-in-configuration)), or enter it on the connection. |
| *The Škoda connection has no API key* / *has no VIN* | Edit the connection and fill in the missing value. |
| An authentication error (401 or 403) | The API key is wrong or has been revoked. Generate a new one in the MyŠkoda app. |
| A validation error (404 or another 4xx) | Check the VIN belongs to the account the API key came from, and that the vehicle supports the command (e.g. auxiliary heating). |
| Rate limiting (429) | The API key's request quota is used up. Automate retries the step after a wait; if it keeps happening, run the automation less often. |
| The Škoda API is unavailable, or the request timed out | Usually temporary: Automate retries the step according to its error behaviour. |

Commands are sent to the car, which can take a while to act on them; a successful step means the Škoda API accepted the command, not that the car has finished it. Use **Get Vehicle Status** in a later step to check.

## Compatibility

| Package version | Umbraco Automate | Umbraco CMS |
|---|---|---|
| 1.x | 17.x – 18.x | 17.4 – 18.x |

One build supports both Umbraco 17 and 18: it's compiled against 17, and every change is tested on both, including running the 17 build on 18. Umbraco 19 isn't supported until it has been tested.

## Links

- [Source code](https://github.com/umbraco-community/Umbraco.Community.Automate/tree/main/Packages/Skoda/Umbraco.Community.Automate.Skoda)
- [Report an issue](https://github.com/umbraco-community/Umbraco.Community.Automate/issues)
