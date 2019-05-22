SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'getXPLIssues')
    DROP PROCEDURE [dbo].getXPLIssues;
GO

CREATE PROCEDURE getXPLIssues
AS
BEGIN
	SET NOCOUNT ON;
	
select a.id,
	   a.name,
	   a.description,
	   a.reportedBy,
	   a.ownerId,
	   c.name as ownerName,
	   a.priority,
	   a.issueTypeId,
	   a.statusId,
	   a.projectId,
	   b.name as projectName,
	   dbo.getJavaTimeStampAsDate(a.createDate) as 'createDate'
from xp_issue as a
inner join xp_project as b on a.projectId = b.id
left outer join xp_user as c on a.ownerId = c.id
where a.projectId in 
	(select id from xp_project)
	
END
GO
