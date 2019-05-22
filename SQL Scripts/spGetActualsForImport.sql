CREATE PROCEDURE spGetActualsForImport AS
BEGIN
	SET NOCOUNT ON;

	SELECT 
		a.AssetOID, 
		a.Value,
		a.Date,
		b.NewAssetOID AS 'Timebox', 
		c.NewAssetOID AS 'Scope', 
		d.NewAssetOID AS 'Member',
		a.Workitem AS 'Workitem',
		f.NewAssetOID AS 'Team'
	FROM Actuals AS a WITH (NOLOCK)
	LEFT OUTER JOIN Iterations AS b ON a.Timebox = b.AssetOID
	LEFT OUTER JOIN Projects AS c ON a.Scope = c.AssetOID
	LEFT OUTER JOIN Members AS d ON a.Member = d.AssetOID
	--LEFT OUTER JOIN Tasks AS e ON a.Workitem = e.AssetOID
	LEFT OUTER JOIN Teams AS f ON a.Team = f.AssetOID

END
GO
