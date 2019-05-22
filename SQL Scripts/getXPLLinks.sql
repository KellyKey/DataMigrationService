SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'getXPLLinks')
    DROP PROCEDURE [dbo].getXPLLinks;
GO

CREATE PROCEDURE getXPLLinks
AS
BEGIN
	SET NOCOUNT ON;

--Get the assets for the 6 projects.
select *
into #tempAssets
from xp_story
where projectId in 
	(select id from xp_project);	
	 
--Get links only for existing assets.
select a.id,
	   a.name as 'linkName',
	   a.storyId,
	   b.name as 'storyName',
	   b.storyTypeId,
	   a.type,
	   a.value,
	   dbo.getJavaTimeStampAsDate(a.createDate) as 'createDate',
	   a.createUserId,
	   c.name as 'ownerName'
from xp_story_links as a
left outer join xp_story as b on a.storyId = b.id
left outer join xp_user as c on a.createUserId = c.id
where a.storyId in (select id from #tempAssets)
and a.type = 'general';

drop table #tempAssets;	
	
END
GO
