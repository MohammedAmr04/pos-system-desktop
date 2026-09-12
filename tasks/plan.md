# Implementation Plan: Local License Trial Management

## Overview

Add local license management for the single-tenant, single-device POS. An authenticated admin can configure the trial duration and license dates. The backend calculates the effective status and blocks the application after expiry; the frontend exposes a settings page for viewing and editing the license.

## Architecture Decisions

- Extend the existing single-row `Settings` table instead of creating a separate `License` table.
- Keep all date validation and expiry decisions in the backend; the frontend only renders the returned status.
- Preserve permanent licenses by allowing a null expiry date.
- Require `license.manage` for updates and continue using the existing local activation flow.
- Keep the trial start date stable after first initialization so repeated status checks cannot extend the trial.

## Acceptance Criteria

- A first installation starts a trial once using the configured number of days.
- Admin can view and update trial days, license type, start date, and expiry date.
- Expired licenses return `locked` and the existing license gate blocks the application.
- Permanent licenses never expire.
- Invalid dates and trial durations are rejected by the backend.
- Machine-ID and clock-rollback protections continue to work.

## Verification

- Backend unit tests cover trial boundaries, permanent licenses, and invalid input.
- `dotnet test backend-cs/Pos.Tests/Pos.Tests.csproj --configuration Release --no-restore`
- `dotnet build backend-cs/pos-cs.csproj --configuration Release --no-restore`
- Frontend typecheck and static build through `build-static.ps1`.
