DROP VIEW UnsoldAsset;

CREATE VIEW UnsoldAsset AS
	SELECT
		COALESCE(tl.[AcquiredDate], t.[When]) AS [AcquiredDate],
		t.[When] AS [TransactionDate],
		t.[Id] AS [TransactionId],
		a.[Id] AS [AssetId],
		tl.[Id] AS [TaxLotId],
		a.[Name] AS [AssetName],
		tl.[CostBasisAmount] AS [CostBasisAmount],
		tl.[CostBasisAssetId] AS [CostBasisAssetId],
		COALESCE(tl.[Amount], te.[Amount]) AS [AcquiredAmount],
		(COALESCE(tl.[Amount], te.[Amount]) - SUM(COALESCE(tla.[Amount],0))) AS [RemainingAmount]
	FROM TransactionEntry te
	JOIN Asset a ON a.Id = te.AssetId
	JOIN [Transaction] t ON t.Id = te.TransactionId
	JOIN TaxLot tl ON tl.CreatingTransactionEntryId = te.Id
	LEFT OUTER JOIN TaxLotAssignment tla ON tla.TaxLotId = tl.Id
	WHERE a.Type = 1
	GROUP BY tl.Id
