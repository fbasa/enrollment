-- Idempotent DB options
IF DB_ID('$(DatabaseName)') IS NULL
BEGIN
  PRINT 'Creating database $(DatabaseName)';
  DECLARE @sql nvarchar(max) = N'CREATE DATABASE [$(DatabaseName)]';
  EXEC(@sql);
END
GO
ALTER DATABASE [$(DatabaseName)] SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
GO
ALTER DATABASE [$(DatabaseName)] SET ALLOW_SNAPSHOT_ISOLATION ON;
GO
