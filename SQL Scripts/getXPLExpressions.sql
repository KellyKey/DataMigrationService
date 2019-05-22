SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'getXPLExpressions')
    DROP PROCEDURE [dbo].getXPLExpressions;
GO

CREATE PROCEDURE getXPLExpressions
AS
BEGIN
	SET NOCOUNT ON;

select *
into #tempAssets
from xp_story
where projectId in 
	(select id from xp_project);	
	 
--Get comments (expressions) only for existing assets.
select a.id as 'expressionId',
	   a.parentId,
	   b.storyTypeId as 'parentTypeId',
	   b.name as 'parentName',
	   a.description,
	   dbo.getJavaTimeStampAsDate(a.createDate) as 'createDate',
	   a.createuserId as 'ownerId',
	   c.name as 'ownerName'
from xp_comment as a
left outer join xp_story as b on a.parentId = b.id
left outer join xp_user as c on a.createuserId = c.id
where a.parentId in (select id from #tempAssets);

drop table #tempAssets;	
	
END
GO
