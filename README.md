# Complaint Tracker

An IT helpdesk ticketing API in ASP.NET Core, backed by
SQL Server through Dapper.

## What it does

Staff and users sign in with a token. Users raise tickets and see their own;
Agents work the queue; Admins assign tickets and can delete them. The listing
endpoint filters by status, category and assignee, searches titles by keyword,
sorts by date or title, and returns results a page at a time.

## Built with

- .NET 8 (ASP.NET Core Web API)
- Dapper 2.1.79 over Microsoft.Data.SqlClient 7.0.2
- SQL Server
- JWT bearer tokens for authentication
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

### 3. Set a signing key

Tokens are signed with a secret that is deliberately not in this repository.
Set your own, from the `ComplaintTracker.Api` folder:

```
dotnet user-secrets set "Jwt:Key" "any long random string, at least 32 characters"
```

It is stored outside the project, so it is never committed. The app refuses to
start without it and tells you this command.

### 4. Run it

```
cd ComplaintTracker.Api
dotnet run
```

Then open <http://localhost:5053/swagger>. From Visual Studio, F5 does the
same and opens the browser for you.

### 5. Run the tests

```
dotnet test
```

94 tests, no database required. Stop the running app first — otherwise the
build cannot overwrite the files it has open.

## Accounts and roles

Every complaint endpoint needs a token. Register, log in, then send the token
as `Authorization: Bearer <token>` — or paste it into Swagger's **Authorize**
button, which does that for you.

| Method | Route | Does |
|---|---|---|
| POST | `/api/auth/register` | Creates an account, always as a `User` |
| POST | `/api/auth/login` | Returns a token valid for 60 minutes |

Three roles decide what an account may do:

| | User | Agent | Admin |
|---|---|---|---|
| Raise a ticket | yes | yes | yes |
| See tickets | own only | all | all |
| Update a ticket | own only | any | any |
| Assign a ticket | no | no | yes |
| Delete a ticket | no | no | yes |

Agents see the whole queue rather than only their own work, so unassigned
tickets can be picked up; `?assignedToMe=true` narrows it to their own.

Registration always creates a `User`. Letting the caller pick a role would let
anyone sign up as an admin, so promote deliberately in the database:

```sql
UPDATE dbo.Users SET Role = 'Admin' WHERE Username = 'you@example.com';
```

The role is carried inside the token, so log in again afterwards — an existing
token keeps the old role until it expires.

Passwords are stored only as a salted hash. Login answers a wrong password and
a username that does not exist identically, so the response cannot be used to
discover which accounts exist.

## The API

Base path: `/api/complaints`

| Method | Route | Does |
|---|---|---|
| GET | `/api/complaints` | List complaints, filtered, sorted and paged |
| GET | `/api/complaints/{id}` | One complaint, or 404 |
| POST | `/api/complaints` | Create one, returns 201 |
| PUT | `/api/complaints/{id}` | Replace one, returns 204 or 404 |
| PUT | `/api/complaints/{id}/assign` | Assign to staff, or unassign with null. Admin only |
| DELETE | `/api/complaints/{id}` | Remove one, returns 204 or 404 |

### Listing options

All optional, and all combinable.

| Parameter | Default | Notes |
|---|---|---|
| `status` | none | Must be one of the four statuses, or 400 |
| `assignedToMe` | `false` | Narrows to tickets assigned to the caller |
| `category` | none | Must be one of the seven categories, or 400 |
| `search` | none | Matched anywhere in the title |
| `sortBy` | `createdDate` | `createdDate` or `title`, else 400 |
| `sortOrder` | `desc` | `asc` or `desc`, else 400 |
| `page` | `1` | 1 or greater, else 400 |
| `pageSize` | `20` | 1 to 100, else 400 |

For example, the open network tickets mentioning "vpn", oldest first, three to a page:

```
GET /api/complaints?status=Open&category=Network&search=vpn&sortOrder=asc&page=1&pageSize=3
```

### A complaint

```json
{
  "id": 1,
  "title": "VPN keeps dropping",
  "description": "Disconnects every few minutes when working from home.",
  "category": "Network",
  "status": "Open",
  "createdDate": "2026-09-10T09:40:28.42Z",
  "raisedBy": "ravi",
  "raisedByUserId": 4,
  "assignedTo": "nawaz",
  "assignedToUserId": 2
}
```

`id` and `createdDate` are set by the server. Leave them out when posting;
anything you send for them is ignored.

`status` must be exactly `Open`, `In Progress`, `Resolved`, or `Closed` —
the spelling and capitalisation matter.
`category` must be exactly one of `Hardware`, `Software`, `Network`,
`Account Access`, `Email`, `Printer`, or `Other`. `title`, `description`,
`category` and `status` are required. `raisedBy`, `raisedByUserId`,
`assignedTo` and `assignedToUserId` are all set by the server: the owner comes
from your token, and the assignee only from the assign endpoint.

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
  Controllers/     ComplaintsController and AuthController — HTTP only, no SQL
  Models/          Complaint, User, PagedResult, and the fixed value lists
  Repositories/    The interfaces and their Dapper implementations
  Services/        TokenService, which signs the login tokens
  Middleware/      Turns unhandled exceptions into ProblemDetails
ComplaintTracker.Api.Tests/
                   xUnit tests, using fake repositories so no database is needed
Database/
  schema.sql       Creates the database, tables and indexes; safe to re-run
```

Controllers deal with HTTP, repositories deal with SQL, and neither knows
about the other's job. Every query is parameterised; the only value that
reaches the SQL text directly is the ORDER BY clause, which is chosen from a
fixed set rather than built from caller input.

## Future enhancements

- Add integration tests covering the SQL against a real database
- Let an Admin promote accounts through the API instead of a SQL UPDATE
- Add refresh tokens, so a session outlives the 60 minute expiry
- Move title search to SQL Server full-text search, which can use an index
  where `LIKE '%keyword%'` cannot
