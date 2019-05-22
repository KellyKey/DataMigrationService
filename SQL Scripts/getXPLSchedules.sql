SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'getXPLSchedules')
    DROP PROCEDURE [dbo].getXPLSchedules;
GO

CREATE PROCEDURE getXPLSchedules
AS
BEGIN

	SET NOCOUNT ON;
	select * from xp_project
	--where statusId <> '7'
	
END
GO
