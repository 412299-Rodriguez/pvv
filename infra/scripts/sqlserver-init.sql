-- PVV — SQL Server initialization script
-- Creates the databases required by pvv-soat and pvv-config if they do not exist.
-- Executed by the sqlserver-init container after SQL Server reports healthy.

IF DB_ID('pvv_soat_db') IS NULL
BEGIN
    PRINT 'Creating database pvv_soat_db...';
    CREATE DATABASE pvv_soat_db;
END
ELSE
BEGIN
    PRINT 'Database pvv_soat_db already exists. Skipping.';
END
GO

IF DB_ID('pvv_config_db') IS NULL
BEGIN
    PRINT 'Creating database pvv_config_db...';
    CREATE DATABASE pvv_config_db;
END
ELSE
BEGIN
    PRINT 'Database pvv_config_db already exists. Skipping.';
END
GO
