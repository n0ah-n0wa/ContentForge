-- Runtime database permissions for the API App Service managed identity.
-- Run as the Azure AD SQL administrator after infrastructure deployment.
--
-- Replace [app-cf-api-dev] with deployment output apiAppName (e.g. app-cf-api-prod).
-- Do NOT grant db_ddladmin here — use grant-api-sql-migration.sql in the deployment pipeline only.

USE [contentforge];
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'app-cf-api-dev')
BEGIN
    CREATE USER [app-cf-api-dev] FROM EXTERNAL PROVIDER;
END
GO

ALTER ROLE db_datareader ADD MEMBER [app-cf-api-dev];
ALTER ROLE db_datawriter ADD MEMBER [app-cf-api-dev];
GO
