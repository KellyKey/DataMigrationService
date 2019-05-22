SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'getXPLTests')
    DROP PROCEDURE [dbo].getXPLTests;
GO

CREATE PROCEDURE getXPLTests
AS
BEGIN
	SET NOCOUNT ON;

select *
into #tempAssets
from xp_story
where projectId in 
	(select id from xp_project);	

--Get tests only for existing assets.
select a.id as 'testId',
	   a.name as 'testName',
	   a.description,
	   a.testTypeId,
	   a.storyId,
	   b.storyTypeId,
	   b.name as 'storyName',
	   a.assigneduserId,
	   c.name as 'ownerName',
	   a.statusId,
	   dbo.getJavaTimeStampAsDate(a.createDate) as 'createDate',
	   dbo.getJavaTimeStampAsDate(a.updateDate) as 'updateDate',
	   a.createuserId,
	   a.updateUserId
from xp_testcase as a
left outer join xp_story as b on a.storyId = b.id
left outer join xp_user as c on a.createuserId = c.id
where storyId in (select id from #tempAssets);

drop table #tempAssets;	
	
END
GO
