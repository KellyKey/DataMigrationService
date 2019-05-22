CREATE PROCEDURE spGetTasksForImport AS
BEGIN
	SET NOCOUNT ON;

	SELECT 
		a.AssetOID, 
		a.AssetState, 
		a.AssetNumber,
		a.Name, 
		a.Description,
		a.Owners,
		a.[Order],
		a.Goals,
		a.Reference,
		a.DetailEstimate,
		a.ToDo,
		a.LastVersion,
		a.Estimate,
		b.NewAssetOID AS 'Category', 
		c.NewAssetOID AS 'Source', 
		d.NewAssetOID AS 'Status',
		e.NewAssetOID AS 'Parent'
	FROM Tasks AS a WITH (NOLOCK)
	LEFT OUTER JOIN ListTypes AS b ON a.Category = b.AssetOID
	LEFT OUTER JOIN ListTypes AS c ON a.Source = c.AssetOID
	LEFT OUTER JOIN ListTypes AS d ON a.Status = d.AssetOID
	LEFT OUTER JOIN Stories AS e ON a.Parent = e.AssetOID
	ORDER BY a.[Order] ASC;

END
GO
