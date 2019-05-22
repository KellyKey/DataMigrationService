SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'getXPLThemes')
    DROP PROCEDURE [dbo].getXPLThemes;
GO

CREATE PROCEDURE getXPLThemes
AS
BEGIN
	SET NOCOUNT ON;
	
select distinct b.name as 'projectName', a.topic
from xp_story as a
inner join xp_project as b
on a.projectId = b.id
where a.projectId in 
	(select id from xp_project)
and a.topic is not null
and a.topic <> ''
order by b.name, a.topic asc;
	
END
GO
