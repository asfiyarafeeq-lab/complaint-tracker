# Complaint Tracker

A small ASP.NET Core Web API for logging and tracking complaints, backed by
SQL Server through Dapper.

## What it does

Complaints are created, read, updated, and deleted over a REST API. The
listing endpoint filters by status and category, searches titles by keyword,
sorts by date or title, and returns results a page at a time.

## Built with

- .NET 8 (ASP.NET Core Web API)
- Dapper 2.1.79 over Microsoft.Data.SqlClient 7.0.2
- SQL Server
- Swagger / Swashbuckle for the browsable API
- xUnit for the tests

## Getting started

### You will need

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (Express or Developer edition is fine) reachable at `localhost`

### 1. Create the database

From the repository root:

```
sqlcmd -S localhost -E -i Database\schema.sql
```

This creates `ComplaintTrackerDb` and the `Complaints` table. It checks
before creating anything and never drops, so running it again is safe and
leaves existing rows alone. You can also open the file in SSMS and press F5.

### 2. Check the connection string

`ComplaintTracker.Api/appsettings.json` expects SQL Server on the local
machine using Windows authentication:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=ComplaintTrackerDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

Change `Server` if yours is elsewhere — a named instance would be
`Server=localhost\SQLEXPRESS`, for example.

### 3. Run it

```
cd ComplaintTracker.Api
dotnet run
```

Then open <http://localhost:5053/swagger>. From Visual Studio, F5 does the
same and opens the browser for you.

### 4. Run the tests

```
dotnet test
```

56 tests, no database required. Stop the running app first — otherwise the
build cannot overwrite the files it has open.

## The API

Base path: `/api/complaints`

| Method | Route | Does |
|---|---|---|
| GET | `/api/complaints` | List complaints, filtered, sorted and paged |
| GET | `/api/complaints/{id}` | One complaint, or 404 |
| POST | `/api/complaints` | Create one, returns 201 |
| PUT | `/api/complaints/{id}` | Replace one, returns 204 or 404 |
| DELETE | `/api/complaints/{id}` | Remove one, returns 204 or 404 |

### Listing options

All optional, and all combinable.

| Parameter | Default | Notes |
|---|---|---|
| `status` | none | Must be one of the four statuses, or 400 |
| `category` | none | Exact match, case-insensitive |
| `search` | none | Matched anywhere in the title |
| `sortBy` | `createdDate` | `createdDate` or `title`, else 400 |
| `sortOrder` | `desc` | `asc` or `desc`, else 400 |
| `page` | `1` | 1 or greater, else 400 |
| `pageSize` | `20` | 1 to 100, else 400 |

For example, the open plumbing complaints mentioning "leak", oldest first,
three to a page:

```
GET /api/complaints?status=Open&category=Plumbing&search=leak&sortOrder=asc&page=1&pageSize=3
```

### A complaint

```json
{
  "id": 1,
  "title": "Water leakage in Block B",
  "description": "Continuous leakage from the overhead tank since Monday.",
  "category": "Plumbing",
  "status": "Open",
  "createdDate": "2026-09-10T09:40:28.42Z",
  "raisedBy": "ravi"
}
```

`id` and `createdDate` are set by the server. Leave them out when posting;
anything you send for them is ignored.

`status` must be exactly `Open`, `In Progress`, `Resolved`, or `Closed` —
the spelling and capitalisation matter. `title`, `category`, `status`, and
`raisedBy` are required, and are capped at 200, 100, 50, and 100 characters
to match the columns.

### A page of complaints

The listing wraps the results, because a page is not much use without
knowing how many there are in total:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 4,
  "totalPages": 1,
  "hasPrevious": false,
  "hasNext": false
}
```

`totalCount` respects the filters, so paging through search results works.

### When something is wrong

Errors come back as `ProblemDetails`, whether they are validation failures
or unexpected faults:

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "sortBy": ["Must be 'createdDate' or 'title'."]
  }
}
```

## Layout

```
ComplaintTracker.Api/
  Controllers/     ComplaintsController — HTTP only, no SQL
  Models/          Complaint, PagedResult, the status and sort values
  Repositories/    IComplaintRepository and its Dapper implementation
  Middleware/      Turns unhandled exceptions into ProblemDetails
ComplaintTracker.Api.Tests/
                   xUnit tests, using a fake repository so no database is needed
Database/
  schema.sql       Creates the database and table; safe to re-run
```

Controllers deal with HTTP, repositories deal with SQL, and neither knows
about the other's job. Every query is parameterised; the only value that
reaches the SQL text directly is the ORDER BY clause, which is chosen from a
fixed set rather than built from caller input.

## Future enhancements

- Constrain `Category` to a fixed set, as `Status` already is
- Add authentication so the endpoints are not open to everyone
- Add integration tests covering the SQL against a real database
- Move title search to SQL Server full-text search, which can use an index
  where `LIKE '%keyword%'` cannot
