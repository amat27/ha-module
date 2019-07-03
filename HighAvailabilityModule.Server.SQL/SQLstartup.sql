USE master;
GO
IF EXISTS (SELECT * FROM sys.databases WHERE NAME='HighAvailabilityModule')
	DROP DATABASE HighAvailabilityModule;
GO
CREATE DATABASE HighAvailabilityModule;
GO

USE HighAvailabilityModule;
GO
IF OBJECT_ID('HeartBeatTable') IS NOT NULL
	DROP TABLE HeartBeatTable;
GO
CREATE TABLE HeartBeatTable
(uuid nvarchar(50),
utype nvarchar(50),
uname nvarchar(50),
timeStamp datetime);
GO

IF OBJECT_ID('HeartBeatInvalid') IS NOT NULL
	DROP FUNCTION HeartBeatInvalid;
GO
CREATE FUNCTION HeartBeatInvalid
(@utype nvarchar(50),
@now datetime)
RETURNS bit
AS
	BEGIN
		DECLARE @InValid bit;
		DECLARE @TimeOut real;
		SET @TimeOut = 1000;
		IF (NOT EXISTS(SELECT * FROM dbo.HeartBeatTable WHERE utype = @utype))
			OR (DATEDIFF(MILLISECOND, (SELECT timeStamp FROM dbo.HeartBeatTable WHERE utype = @utype), @now) >= @TimeOut)
			SET @InValid = 1;
		ELSE
			SET @InValid = 0;
		RETURN @InValid
	END
GO

IF OBJECT_ID('LastSeenEntryValid') IS NOT NULL
	DROP FUNCTION LastSeenEntryValid;
GO
CREATE FUNCTION LastSeenEntryValid
(@utype nvarchar(50), 
@lastSeenUuid nvarchar(50), 
@lastSeenUtype nvarchar(50), 
@lastSeenTimeStamp datetime)
RETURNS bit
AS
	BEGIN
		 DECLARE @IsValid bit;
		 IF (EXISTS(SELECT * FROM dbo.HeartBeatTable WHERE utype = @utype)) 
			AND ((SELECT uuid FROM dbo.HeartBeatTable WHERE utype = @utype) = @lastSeenUuid)
			AND ((SELECT utype FROM dbo.HeartBeatTable WHERE utype = @utype) = @lastSeenUtype) 
			AND ((SELECT timeStamp FROM dbo.HeartBeatTable WHERE utype = @utype) = @lastSeenTimeStamp)
				SET @IsValid = 1;
		ELSE
			SET @IsValid = 0;
	RETURN @IsValid
	END
GO

IF OBJECT_ID('ValidInput') IS NOT NULL
	DROP FUNCTION ValidInput;
GO
CREATE FUNCTION ValidInput
(@uuid nvarchar(50), 
@utype nvarchar(50), 
@lastSeenUuid nvarchar(50), 
@lastSeenUtype nvarchar(50), 
@lastSeenTimeStamp datetime,
@now datetime)
RETURNS bit
AS
	BEGIN
		DECLARE @IsValid bit;
		DECLARE @TimeDefault datetime;
		SET @TimeDefault = CONVERT(DATETIME,'1753-01-01 12:00:00.000',21);
		IF (NOT EXISTS(SELECT * FROM dbo.HeartBeatTable WHERE utype = @utype)) 
			OR (dbo.HeartBeatInvalid(@utype, @now) = 1 AND (@lastSeenUuid = '') AND (@lastSeenUtype = '') AND(@lastSeenTimeStamp = @TimeDefault))
			OR (dbo.LastSeenEntryValid(@utype, @lastSeenUuid, @lastSeenUtype, @lastSeenTimeStamp) = 1 
			AND ((SELECT uuid FROM dbo.HeartBeatTable WHERE utype = @utype) = @uuid) AND ((SELECT utype FROM dbo.HeartBeatTable WHERE utype=@utype)=@utype))
			SET @IsValid = 1;
		ELSE
			SET @IsValid = 0;
		RETURN @IsValid
	END
GO

USE HighAvailabilityModule;
GO
IF OBJECT_ID('HeartBeatAsync') IS NOT NULL
	DROP PROCEDURE HeartBeatAsync;
GO
CREATE PROCEDURE HeartBeatAsync
	@uuid nvarchar(50),
	@utype nvarchar(50),
	@uname nvarchar(50),
	@lastSeenUuid nvarchar(50),
	@lastSeenUtype nvarchar(50),
	@lastSeenTimeStamp datetime
AS
	SET NOCOUNT ON;
	DECLARE @now datetime;
	SET @now = CONVERT(DATETIME, GETDATE(), 21);
	IF dbo.ValidInput(@uuid, @utype, @lastSeenUuid, @lastSeenUtype, @lastSeenTimeStamp, @now) = 1
		BEGIN
			IF NOT EXISTS (SELECT * FROM dbo.HeartBeatTable WHERE utype = @utype)
				INSERT INTO dbo.HeartBeatTable(uuid, utype, uname, timeStamp)
				VALUES(@uuid, @utype, @uname, @now);
			ELSE
				UPDATE dbo.HeartBeatTable
				SET uuid = @uuid, utype = @utype, uname = @uname, timeStamp = @now
				WHERE utype = @utype;
		END
GO

USE HighAvailabilityModule;
GO
IF OBJECT_ID('GetHeartBeatAsync') IS NOT NULL
	DROP PROCEDURE GetHeartBeatAsync;
GO
CREATE PROCEDURE GetHeartBeatAsync
	@utype nvarchar(50)
AS
	SET NOCOUNT ON
	DECLARE @now datetime;
	SET @now = CONVERT(DATETIME, GETDATE(), 21);
	IF dbo.HeartBeatInvalid(@utype, @now) = 1
		SELECT * FROM dbo.HeartBeatTable WHERE utype = NULL;
	ELSE
		SELECT * FROM dbo.HeartBeatTable WHERE utype=@utype;
GO