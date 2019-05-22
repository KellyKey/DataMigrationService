CREATE PROCEDURE spGetLinksForImport AS
BEGIN
	SET NOCOUNT ON;

	SELECT * FROM Links WITH (NOLOCK)
	WHERE Asset IN (SELECT assetOID FROM Members)
	AND Asset LIKE 'Member%'

	UNION ALL

	SELECT * FROM Links WITH (NOLOCK)
	WHERE Asset IN (SELECT assetOID FROM Teams)
	AND Asset LIKE 'Team%'

	UNION ALL

	SELECT * FROM Links WITH (NOLOCK)
	WHERE Asset IN (SELECT assetOID FROM Schedules)
	AND Asset LIKE 'Schedule%'

	UNION ALL

	SELECT * FROM Links WITH (NOLOCK)
	WHERE Asset IN (SELECT assetOID FROM Projects)
	AND Asset LIKE 'Scope%'

	UNION ALL

	SELECT * FROM Links WITH (NOLOCK)
	WHERE Asset IN (SELECT assetOID FROM Iterations)
	AND Asset LIKE 'Timebox%'

	UNION ALL

	SELECT * FROM Links WITH (NOLOCK)
	WHERE Asset IN (SELECT assetOID FROM Goals)
	AND Asset LIKE 'Goal%'

	UNION ALL

	SELECT * FROM Links WITH (NOLOCK)
	WHERE Asset IN (SELECT assetOID FROM FeatureGroups)
	AND Asset LIKE 'Theme%'

	UNION ALL

	SELECT * FROM Links WITH (NOLOCK)
	WHERE Asset IN (SELECT assetOID FROM Requests)
	AND Asset LIKE 'Request%'

	UNION ALL

	SELECT * FROM Links WITH (NOLOCK)
	WHERE Asset IN (SELECT assetOID FROM Issues)
	AND Asset LIKE 'Issue%'

	UNION ALL

	-- NOTE: Support for 11.3 and earlier (epic is story).
	SELECT * FROM Links WITH (NOLOCK)
	WHERE Asset IN (SELECT assetOID FROM Epics)
	AND Asset LIKE 'Story%'
	OR Asset LIKE 'Epic%'

	UNION ALL

	SELECT * FROM Links WITH (NOLOCK)
	WHERE Asset IN (SELECT assetOID FROM Stories)
	AND Asset LIKE 'Story%'

	UNION ALL

	SELECT * FROM Links WITH (NOLOCK)
	WHERE Asset IN (SELECT assetOID FROM Defects)
	AND Asset LIKE 'Defect%'

	UNION ALL

	SELECT * FROM Links WITH (NOLOCK)
	WHERE Asset IN (SELECT assetOID FROM Tasks)
	AND Asset LIKE 'Task%'

	UNION ALL

	SELECT * FROM Links WITH (NOLOCK)
	WHERE Asset IN (SELECT assetOID FROM Tests)
	AND Asset LIKE 'Test%'

END
GO
