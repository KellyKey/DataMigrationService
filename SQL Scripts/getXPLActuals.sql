SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'getXPLActuals')
    DROP PROCEDURE [dbo].getXPLActuals;
GO

CREATE PROCEDURE getXPLActuals
AS
BEGIN
	SET NOCOUNT ON;
	
select *
into #tempAssets
from xp_story
where projectId in 
	(select id from xp_project);	

/* MATT'S CODE:
select 'Actual' as AssetType, 
       CONVERT(varchar(255), xpt.name) + ' (' + xpt.id + ')' as Workitem,
       xpt.completedHours as Value,
       ISNULL(xpua.name,'Member:20') as Member,
       xpt.storyId as Scope,
       ISNULL(dbo.getJavaTimeStampAsDate(xpt.updateDate),dbo.getJavaTimeStampAsDate(xpt.createDate)) as Date
  from xp_task xpt
  join xp_story xps on xpt.storyId = xps.id
  left outer join xp_user xpua on xpt.assigneduserId = xpua.id
  join xp_project xpp on xps.projectId = xpp.id
  where CONVERT(numeric, xpt.completedHours) > 0
*/

--Get actuals only for existing assets.
select a.id as 'taskId',
	   a.name as 'taskName',
	   a.completedHours,
	   a.storyId,
	   b.name as 'storyName',
	   b.releaseId,
	   d.name as 'releaseName',
	   b.projectId,
	   e.name as 'projectName',
	   a.assigneduserId as 'assignedUserId',
	   a.createuserId as 'OwnerId',
	   c.name as 'ownerName',
	   dbo.getJavaTimeStampAsDate(a.createDate) as 'createDate',
	   dbo.getJavaTimeStampAsDate(a.updateDate) as 'updateDate'
from xp_task as a
left outer join xp_story as b on a.storyId = b.id
left outer join xp_user as c on a.assigneduserId = c.id
left outer join xp_release as d on b.releaseId = d.id
left outer join xp_project as e on b.projectId = e.id
where storyId in (select id from #tempAssets)
and CONVERT(numeric, a.completedHours) > 0;

drop table #tempAssets;	
	
END
GO
