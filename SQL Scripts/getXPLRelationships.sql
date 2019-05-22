SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'getXPLRelationships')
    DROP PROCEDURE [dbo].getXPLRelationships;
GO

CREATE PROCEDURE getXPLRelationships
AS
BEGIN
	SET NOCOUNT ON;

select *
into #tempAssets
from xp_story
where projectId in 
	(select id from xp_project);	
	 
--Get links only for existing assets.
select a.id as 'linkId',
	   a.type as 'linkType',
	   a.name as 'linkName',
	   a.storyId as 'assetId',
	   b.name as 'assetName',
	   b.storyTypeId as 'assetType',
	   a.value as 'valueId',
	   c.name as 'valueName',
	   c.storyTypeId as 'valueType'
from xp_story_links as a
left outer join xp_story as b on a.storyId = b.id
left outer join xp_story as c on a.value = c.id
where a.storyId in (select id from #tempAssets)
and a.type <> 'general'
and c.name is not null;

drop table #tempAssets;	
	
END
GO
