SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'getXPLTasks')
    DROP PROCEDURE [dbo].getXPLTasks;
GO

CREATE PROCEDURE getXPLTasks
AS
BEGIN
	SET NOCOUNT ON;
	
--Get the assets for the 6 projects.
select *
into #tempAssets
from xp_story
where projectId in 
	(select id from xp_project);	

--Get tasks only for existing assets.
select a.id as 'taskId',
	   a.name as 'taskName',
	   a.description,
	   a.originalHours,
	   a.estimatedHours,
	   a.completedHours,
	   a.taskTypeId,
	   a.storyId,
	   b.storyTypeId,
	   b.name as 'storyName',
	   a.assigneduserId as 'assignedUserId',
	   c.name as 'ownerName',
	   a.statusId,
	   dbo.getJavaTimeStampAsDate(a.createDate) as 'createDate',
	   dbo.getJavaTimeStampAsDate(a.updateDate) as 'updateDate',
	   a.createuserId,
	   a.updateUserId,
	   a.remainingHours,
	   a.pairUserId
from xp_task as a
left outer join xp_story as b on a.storyId = b.id
left outer join xp_user as c on a.assigneduserId = c.id
where storyId in (select id from #tempAssets);

drop table #tempAssets;	
	
END
GO
