EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;
GO
EXEC sp_configure 'clr enabled'

USE HighAvailabilityWitness
GO
ALTER DATABASE HighAvailabilityWitness SET TRUSTWORTHY ON;

