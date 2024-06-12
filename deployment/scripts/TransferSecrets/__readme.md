# Save and restore secrets

A quick example for use scripts in a sensenet Repository reinstallation (powershell).
``` Powershell
# Step-1: Ensure transfer database
> Invoke-Sqlcmd -ServerInstance ".\SQL2016" -Database "ReinstallationTransfer" -InputFile  Install.sql

# Step-1: Save secrets
> Invoke-Sqlcmd -ServerInstance ".\SQL2016" -Database "SnWebApplication.Api.Sql.TokenAuth3" -InputFile  Save.sql

#
# Step-2: Drop and install database with same server and name
#

# Step-3: Restore secrets
> Invoke-Sqlcmd -ServerInstance ".\SQL2016" -Database "SnWebApplication.Api.Sql.TokenAuth3" -InputFile  Restore.sql
```

Scripts
- **Cleanup.sql**: Clears all records from all tables in the ReinstallationTransfer database.
- **Install.sql**: Ensures the required tables in the ReinstallationTransfer database.
- **Restore.sql**: Restores the saved secrets to the current database from the ReinstallationTransfer database.
- **Save.sql**: Saves secrets from the current database to the ReinstallationTransfer database.
- **_check.sql**: Contains SQL scripts for checking data
- **_dropTables.sql**: Drops the tabled from the ReinstallationTransfer database.
- **_hack.sql**: Renames records in the ReinstallationTransfer (use with caution).
