---
name: efcore-migrations
description: Use this skill when creating, applying, rolling back, scripting, or troubleshooting Entity Framework Core database migrations and schema updates for the MihiraStores application.
---

# EF Core Migrations Skill

Procedure:
1. Inspect the current project and configured provider.
2. Confirm the application uses SQL Server.
3. Review entity relationships, foreign keys, indexes, and delete behaviour.
4. Ensure Stock has a unique constraint/index on (BranchId, ProductId).
5. Create a migration with a descriptive name.
6. Inspect the generated migration before applying it.
7. Apply it to the intended development database only.
8. Run the application or a schema check to verify the expected tables and indexes.
9. Run dotnet build and dotnet test.
10. Report commands executed and their real outcomes.

Do not silently switch the application to SQLite.
Do not delete or recreate a database containing data without explicit approval.
Do not claim migration success if the command failed or was not run.