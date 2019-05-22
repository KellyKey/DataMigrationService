SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'getXPLEpics')
    DROP PROCEDURE [dbo].getXPLEpics;
GO

CREATE PROCEDURE getXPLEpics
AS
BEGIN
	SET NOCOUNT ON;
	
--Get the assets for the 6 projects.
select *
into #tempAssets
from xp_story
where projectId in 
	(select id from xp_project);	
	
--Remove any asset that is not an epic.
delete from #tempAssets 
where name not like '%EPIC%'
and name not like '%Epic%';

--Join to other tables to get full data.
select a.id as 'epicId',
	   a.name as 'epicName',
	   a.priority,
	   a.topic,
	   a.description,
	   a.value,
	   a.risk,
	   a.customer,
	   a.ownerId,
	   d.name as 'ownerName',
	   a.estimatedHours,
	   a.storyTypeId,
	   a.reference,
	   a.releaseId,
	   e.name as 'releaseName',
	   a.iterationId,
	   b.name as 'iterationName',
	   a.projectId,
	   c.name as 'projectName',
	   a.statusId,
	   dbo.getJavaTimeStampAsDate(a.createDate) as 'createDate',
	   dbo.getJavaTimeStampAsDate(a.updateDate) as 'updateDate',
	   a.createuserId,
	   a.updateUserId
from #tempAssets as a
left outer join xp_iteration as b on a.iterationId = b.id
left outer join xp_project as c on a.projectId = c.id
left outer join xp_user as d on a.ownerId = d.id
left outer join xp_release as e on a.releaseId = e.id;

drop table #tempAssets;	
	
END
GO
