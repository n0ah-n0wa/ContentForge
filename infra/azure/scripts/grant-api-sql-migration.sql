-- Migration-time database permissions for the API App Service managed identity.
-- Run only from a controlled deployment pipeline step, then prefer a dedicated
-- migration runner identity with db_ddladmin instead of the runtime API identity.
--
-- Replace [app-cf-api-dev] with deployment output apiAppName when the pipeline
-- uses the API managed identity for EF Core migrations.

USE [contentforge];
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'app-cf-api-dev')
BEGIN
    CREATE USER [app-cf-api-dev] FROM EXTERNAL PROVIDER;
END
GO

ALTER ROLE db_ddladmin ADD MEMBER [app-cf-api-dev];
GO

-- After migrations complete in production, consider revoking ddladmin from the
-- runtime API identity and using a separate pipeline service principal instead.
