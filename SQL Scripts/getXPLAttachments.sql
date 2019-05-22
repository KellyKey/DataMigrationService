SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'getXPLAttachments')
    DROP PROCEDURE [dbo].getXPLAttachments;
GO

CREATE PROCEDURE getXPLAttachments
AS
BEGIN
	SET NOCOUNT ON;

--Build a table of various asset types for the given projects.
--This is joined to get the asset id and name.
create table #tempAssets
(
	assetId varchar(255),
	assetName varchar(255),
	assetType varchar(255)
)

insert into #tempAssets
select id, name, storyTypeId
from xp_story
where projectId in 
  (select id from xp_project);	
	
insert into #tempAssets
select id, name, NULL
from xp_issue
where projectId in 
  (select id from xp_project);
	
insert into #tempAssets
select id, name, NULL
from xp_task
where storyId in 
  (select assetId from #tempAssets);
		
--select * from #tempAssets
--drop table #tempAssets;	
	 
select a.id,
	   a.name as 'attachmentName',
	   a.description,
	   a.filepath,
	   a.filesize,
	   a.contentType,
	   a.parentId as 'assetId',
	   b.assetName,
	   b.assetType,
	   dbo.getJavaTimeStampAsDate(a.createDate) as 'createDate',
	   a.createuserId as 'ownerId',
	   c.name as 'ownerName'
from xp_note as a
left outer join #tempAssets as b on a.parentId = b.assetId
left outer join xp_user as c on a.createuserId = c.id
where a.parentId in (select assetId from #tempAssets)

drop table #tempAssets;	
	
END
GO
