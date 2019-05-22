CREATE PROCEDURE spGetConversationsForImport AS
BEGIN
	SET NOCOUNT ON;

	SELECT * FROM Conversations WITH (NOLOCK)
	WHERE BaseAssets IN (SELECT assetOID FROM Members)
	AND BaseAssets LIKE 'Member%'
	AND Author IS NOT NULL

	UNION ALL

	SELECT * FROM Conversations WITH (NOLOCK)
	WHERE BaseAssets IN (SELECT assetOID FROM Teams)
	AND BaseAssets LIKE 'Team%'
	AND Author IS NOT NULL

	UNION ALL

	SELECT * FROM Conversations WITH (NOLOCK)
	WHERE BaseAssets IN (SELECT assetOID FROM Schedules)
	AND BaseAssets LIKE 'Schedule%'
	AND Author IS NOT NULL

	UNION ALL

	SELECT * FROM Conversations WITH (NOLOCK)
	WHERE BaseAssets IN (SELECT assetOID FROM Projects)
	AND BaseAssets LIKE 'Scope%'
	AND Author IS NOT NULL

	UNION ALL

	SELECT * FROM Conversations WITH (NOLOCK)
	WHERE BaseAssets IN (SELECT assetOID FROM Programs)
	AND BaseAssets LIKE 'ScopeLabel%'
	AND Author IS NOT NULL

	UNION ALL

	SELECT * FROM Conversations WITH (NOLOCK)
	WHERE BaseAssets IN (SELECT assetOID FROM Iterations)
	AND BaseAssets LIKE 'Timebox%'
	AND Author IS NOT NULL

	UNION ALL

	SELECT * FROM Conversations WITH (NOLOCK)
	WHERE BaseAssets IN (SELECT assetOID FROM Goals)
	AND BaseAssets LIKE 'Goal%'
	AND Author IS NOT NULL

	UNION ALL

	SELECT * FROM Conversations WITH (NOLOCK)
	WHERE BaseAssets IN (SELECT assetOID FROM FeatureGroups)
	AND BaseAssets LIKE 'Theme%'
	AND Author IS NOT NULL

	UNION ALL

	SELECT * FROM Conversations WITH (NOLOCK)
	WHERE BaseAssets IN (SELECT assetOID FROM Requests)
	AND BaseAssets LIKE 'Request%'
	AND Author IS NOT NULL

	UNION ALL

	SELECT * FROM Conversations WITH (NOLOCK)
	WHERE BaseAssets IN (SELECT assetOID FROM Issues)
	AND BaseAssets LIKE 'Issue%'
	AND Author IS NOT NULL

	UNION ALL

	-- NOTE: Support for 11.3 and earlier (epic is story).
	SELECT * FROM Conversations WITH (NOLOCK)
	WHERE BaseAssets IN (SELECT assetOID FROM Epics)
	AND BaseAssets LIKE 'Story%'
	OR BaseAssets LIKE 'Epic%'
	AND Author IS NOT NULL

	UNION ALL

	SELECT * FROM Conversations WITH (NOLOCK)
	WHERE BaseAssets IN (SELECT assetOID FROM Stories)
	AND BaseAssets LIKE 'Story%'
	AND Author IS NOT NULL

	UNION ALL

	SELECT * FROM Conversations WITH (NOLOCK)
	WHERE BaseAssets IN (SELECT assetOID FROM Defects)
	AND BaseAssets LIKE 'Defect%'
	AND Author IS NOT NULL

	UNION ALL

	SELECT * FROM Conversations WITH (NOLOCK)
	WHERE BaseAssets IN (SELECT assetOID FROM Tasks)
	AND BaseAssets LIKE 'Task%'
	AND Author IS NOT NULL

	UNION ALL

	SELECT * FROM Conversations WITH (NOLOCK)
	WHERE BaseAssets IN (SELECT assetOID FROM Tests)
	AND BaseAssets LIKE 'Test%'
	AND Author IS NOT NULL

END
GO
