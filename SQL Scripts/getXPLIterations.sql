SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'getXPLIterations')
    DROP PROCEDURE [dbo].getXPLIterations;
GO

CREATE PROCEDURE getXPLIterations
AS
BEGIN
	SET NOCOUNT ON;
	
select a.id as 'iterationId',
	   a.name as 'iterationName', 
	   a.description, 
	   dbo.getJavaTimeStampAsDate(a.startDate) as 'startDate', 
       dbo.getJavaTimeStampAsDate(a.endDate) as 'endDate',
       a.projectId,
       b.name as 'projectName',
       a.statusId,
       dbo.getJavaTimeStampAsDate(a.createDate) as 'createDate',
       dbo.getJavaTimeStampAsDate(a.updateDate) as 'updateDate'
from xp_iteration as a
inner join xp_project as b
on a.projectId = b.id
where a.projectId in 
	(select id from xp_project
	 where [name] not like '%OBSOLETE%')
and a.name not in 
	('KILLER STORIES', 
	 'EPIC', 
	 'Backlog', 
	 'EPIC STORIES', 
	 'To Delete', 
	 'Beyond Backlog Iteration DQ', 
	 'backlog', 
	 'Arch-Backlog', 
	 'EPIC 9.0.1',
	 'Move to Future',
	 'EPIC 9.0.2',
	 '9.0.2 backlog',
	 'EPIC 9.0.1 HF1',
	 '2012-07-Beta',
	 'Arch-2: DQT Functionality Design',
	 'Alpha',
	 'HDP',
	 'ChangeControl',
	 'ChangeControlApproved',
	 'Deleted/Dropped',
	 '951-Out-Of-Scope',
	 'to delete',
	 '9.0.3 HF1 - Jan15'
	 )
and a.startDate is not null
order by dbo.getJavaTimeStampAsDate(a.startDate) asc
	
END
GO
