CREATE TABLE "TaxLot" (
	"Id"                         INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
	"CreatingTransactionEntryId" INTEGER NOT NULL REFERENCES "TransactionEntry"("Id") ON DELETE CASCADE,
	"AcquiredDate"               INTEGER, -- when null, use the TransactionEntry's date
	"CostBasisAmount"            REAL,
	"CostBasisAssetId"           REAL
);

CREATE TABLE "TaxLotAssignment" (
	"Id"                          INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
	"TaxLotId"                    INTEGER NOT NULL REFERENCES "TaxLot"("Id") ON DELETE CASCADE,
	"ConsumingTransactionEntryId" INTEGER NOT NULL REFERENCES "TransactionEntry"("Id") ON DELETE CASCADE,
	"Amount"                      REAL NOT NULL,
	"Pinned"                      INTEGER NOT NULL DEFAULT(0)
);

CREATE INDEX "TaxLotAssignment_ConsumingTransactionEntryId" ON [TaxLotAssignment]("ConsumingTransactionEntryId");
CREATE INDEX "TaxLotAssignment_TaxLotId" ON [TaxLotAssignment]("TaxLotId");
CREATE UNIQUE INDEX "TaxLotAssignment_TaxLotId_ConsumingTransactionEntryId" ON [TaxLotAssignment]("TaxLotId", "ConsumingTransactionEntryId");

CREATE VIEW UnsoldAsset AS
	SELECT
		t.[When] AS [AcquiredDate],
		t.[Id] AS [TransactionId],
		a.[Id] AS [AssetId],
		tl.[Id] AS [TaxLotId],
		a.[Name] AS [AssetName],
		te.[Amount] AS [AcquiredAmount],
		(te.[Amount] - SUM(COALESCE(tla.[Amount],0))) AS [RemainingAmount]
	FROM TransactionEntry te
	JOIN Asset a ON a.Id = te.AssetId
	JOIN [Transaction] t ON t.Id = te.TransactionId
	JOIN TaxLot tl ON tl.CreatingTransactionEntryId = te.Id
	LEFT OUTER JOIN TaxLotAssignment tla ON tla.TaxLotId = tl.Id
	WHERE a.Type = 1
	GROUP BY tl.Id
	HAVING RemainingAmount > 0;
-- TODO: for purposes of UI presentation, add a filter for transaction 
--       so that the RemainingAmount subtotal can exclude the transaction being shown,
--       since it will have a unique column dedicated to showing (and editing) that transaction.
-- TODO: How will tax lots work with transfers across accounts?
-- USE CASES:
--  * tax lots can be opened 
--       ✅ purchase of shares
--       ✅ adding of shares (without a purchase)
--       ⏹️ short sale
--  * tax lots can be closed by 
--       ✅ sale of shares
--       ✅ removal of shares (without a sale)
--       ⏹️ covering a short sale
--  * ⏹️ tax lots must track a transfer of shares from one account to another.
--  * ⏹️ Display unrealized losses and gains, *by account*.
--  * ⏹️ Isolate tax lots to their accounts where important (401k, brokerage), but allow for transfers across accounts (crypto).
--    We could say that tax lots are 'locked' into the account they are created inside. When shares are transferred,
--    that tax lot is closed and another created.
--    When selecting tax lot(s) to close or take from in a transaction, only those assigned to that account 
--    and with acquisition dates no newer than the closing date are available for selection.
