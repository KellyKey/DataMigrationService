SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'getXPLReleases')
    DROP PROCEDURE [dbo].getXPLReleases;
GO

CREATE PROCEDURE getXPLReleases
AS
BEGIN
	SET NOCOUNT ON;
	
select a.id, 
	   a.name, 
	   a.description, 
	   a.projectId, 
	   b.name as projectName, 
	   a.statusId,
	   dbo.getJavaTimeStampAsDate(a.releaseDate) as 'releaseDate',
	   dbo.getJavaTimeStampAsDate(a.createDate) as 'createDate',
	   dbo.getJavaTimeStampAsDate(a.updateDate) as 'updateDate',
	   a.createuserId,
	   a.updateUserId,
	   a.capacity
from xp_release as a
inner join xp_project as b
on a.projectId = b.id
where a.projectId in 
	(select id from xp_project);
	
END
GO
