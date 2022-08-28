CREATE TABLE "TaxLot" (
	"Id"                         INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
	"CreatingTransactionEntryId" INTEGER NOT NULL REFERENCES "TransactionEntry"("Id") ON DELETE CASCADE,
	"AcquiredDate"               INTEGER -- when null, use the TransactionEntry's date
);

CREATE TABLE "TaxLotAssignment" (
	"Id"                          INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
	"TaxLotId"                    INTEGER NOT NULL REFERENCES "TaxLot"("Id") ON DELETE CASCADE,
	"ConsumingTransactionEntryId" INTEGER NOT NULL REFERENCES "TransactionEntry"("Id") ON DELETE CASCADE,
	"Amount"                      REAL NOT NULL
);

CREATE INDEX "TaxLotAssignment_EntryIds" ON [TaxLotAssignment]("AcquiredTransactionEntryId", "DispenseTransactionEntryId");

CREATE VIEW ListUnsoldAssets AS
	SELECT t.[When] AS [AcquiredDate], t.[Id] AS [TransactionId], a.[Id] AS [AssetId], a.[Name] AS [AssetName], te.[Amount] AS [AcquiredAmount], (te.[Amount] - SUM(tl.[Amount])) AS [RemainingAmount]
	FROM TransactionEntry te
	JOIN Asset a ON a.Id = te.AssetId
	JOIN [Transaction] t ON t.Id = te.TransactionId
	LEFT OUTER JOIN TaxLotAssignment tl ON tl.AcquiredTransactionEntryId = te.Id
	WHERE a.Type = 1;
-- TODO: add a column with cost basis / price information for the purchase
-- TODO: for purposes of UI presentation, add a filter for transaction 
--       so that the RemainingAmount subtotal can exclude the transaction being shown,
--       since it will have a unique column dedicated to showing (and editing) that transaction.
-- TODO: How will tax lots work with transfers across accounts?
-- TODO: We may want to allow the user to Add shares with cost basis and purchase date information.
-- USE CASES:
--  * tax lots can be opened 
--       purchase of shares
--       adding of shares (without a purchase)
--       short sale
--  * tax lots can be closed by 
--       sale of shares
--       covering a short sale
--       removal of shares (without a sale)
--  * tax lots must track a transfer of shares from one account to another.
--  * Display unrealized losses and gains, *by account*.
--  * Isolate tax lots to their accounts where important (401k, brokerage), but allow for transfers across accounts (crypto).
--    We could say that tax lots are 'locked' into the account they are created inside. When shares are transferred,
--    that tax lot is closed and another created. 
--    When selecting tax lot(s) to close or take from in a transaction, only those assigned to that account are available for selection.
