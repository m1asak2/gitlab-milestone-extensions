# GitLab Milestone Dashboard - Codex Instructions

## 重要な前提

このリポジトリは **Visual Studio の Aspire Starter App** をベースにしています。

そのため、以下は維持してください。

- `AppHost`
- `ServiceDefaults`

不要なサンプルコードやテンプレート生成物は削除して構いませんが、Aspire の土台は残してください。
## Goal
Build a read-only dashboard for GitLab CE milestones, issues, and assignees.

## Tech Stack
- Backend: ASP.NET Core Minimal API
- Frontend: Blazor WebAssembly
- UI: MudBlazor
- Language: C#
- API documentation: OpenAPI
- Cache: IMemoryCache

## Scope
The app only displays data.
No create/update/delete operations against GitLab.
No local editing UI is required.

## Data Source
Use GitLab REST API from the backend only.
The Web project must never call GitLab directly.

## Primary Features
1. Summary dashboard
2. Gantt-like view
   - By group
   - By project
   - By assignee
3. Table view
   - Milestones
   - Issues
4. Filtering
   - Group
   - Project
   - Milestone
   - Assignee
   - State
   - Date range

## Architecture Rules
- Keep Program.cs thin.
- Use endpoint mapping extension methods.
- Put GitLab HTTP access in Infrastructure only.
- Put aggregation/query logic in Application only.
- Keep Domain free of infrastructure concerns.
- DTOs are in Application.
- UI view models stay in Web only.
- Use async/await everywhere.
- Add CancellationToken to all IO methods.
- Public classes and methods should have XML comments.

## API Design Rules
- Use read-only GET endpoints only for MVP.
- Return ProblemDetails on unexpected errors.
- Enable OpenAPI.
- Keep endpoint names stable and simple.

## UI Rules
- Use MudBlazor for layout, filters, cards, tables, tabs.
- Do not overuse JS interop.
- Gantt can start as a simple custom timeline component.

## Testing Rules
- Prioritize Application and Infrastructure tests.
- Mock GitLab API responses in tests.
- Do not depend on a live GitLab server for CI.
- For each instructed change, add or update tests and verify they pass.

## Non-Goals
- Editing milestones/issues
- GitLab webhook processing
- Background jobs
- Database persistence for MVP

## First Deliverables
1. Solution and project structure
2. Minimal API endpoints
3. GitLab API client abstraction
4. Summary API
5. Issues table API
6. Simple MudBlazor dashboard page
