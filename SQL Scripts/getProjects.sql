USE V1Migration;
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'getProjects')
    DROP PROCEDURE [dbo].getProjects;
GO

CREATE PROCEDURE getProjects
AS
BEGIN
	SET NOCOUNT ON;
	select * from Projects;
END
GO
