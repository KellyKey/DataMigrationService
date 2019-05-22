SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'getXPLStories')
    DROP PROCEDURE [dbo].getXPLStories;
GO

CREATE PROCEDURE getXPLStories
AS
BEGIN
	SET NOCOUNT ON;

--Get the assets for the 6 projects.
select *
into #tempAssets
from xp_story
where projectId in 
	(select id from xp_project);	
	
--Remove any asset that is not a story.
--delete from #tempAssets where name like '%EPIC%'
--delete from #tempAssets where name like '%Epic%';

-- Delete sotries that are actually defects.
delete from #tempAssets where storyTypeId = 2;

--Join to other tables to get full data.
select a.id as 'storyId',
	   a.name as 'storyName',
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
