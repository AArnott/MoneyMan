CREATE TABLE "Asset" (
	"Id"   INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
	"Name" TEXT NOT NULL UNIQUE,
	"TickerSymbol" TEXT,
	"Type" INTEGER NOT NULL DEFAULT(1),
	"CurrencySymbol" TEXT,
	"CurrencyDecimalDigits" INTEGER
);

-- We normally wouldn't insert novel data into the database in an upgrade step, but
-- all transactions inserted in prior versions made an assumption about the currency
-- and now the database records it explicitly, so we have to explicitly encode the assumption.
INSERT INTO "Asset" VALUES(1, "United States Dollar", "USD", 0, "$", 2);

ALTER TABLE "Account" ADD "Type" INTEGER NOT NULL DEFAULT(0);

-- Asset.Id of the currency used for this account if Type = 0 (Banking).
ALTER TABLE "Account" ADD "CurrencyAssetId" INTEGER REFERENCES "Asset"("Id") ON DELETE RESTRICT;
UPDATE "Account" SET "CurrencyAssetId" = 1 WHERE Type = 0;

CREATE TABLE "Configuration" (
	"PreferredAssetId"    INTEGER REFERENCES "Asset"("Id")           ON DELETE RESTRICT
);

INSERT INTO "Configuration" VALUES (1);

------ TRANSACTION table modification

PRAGMA foreign_keys = off;

CREATE TABLE "Transaction_new" (
	"Id"                  INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
	"When"                INTEGER NOT NULL,
	"Action"              INTEGER NOT NULL,
	"CheckNumber"         INTEGER,
	"Memo"                TEXT,
	"Payee"               TEXT,
	"CategoryId"          INTEGER REFERENCES "Category"("Id")        ON DELETE SET NULL,

	"CreditAccountId"     INTEGER REFERENCES "Account"("Id")         ON DELETE SET NULL,
	"CreditAmount"        REAL,
	"CreditAssetId"       INTEGER REFERENCES "Asset"("Id")           ON DELETE RESTRICT,
	"CreditCleared"       INTEGER NOT NULL DEFAULT(0),

	"DebitAccountId"      INTEGER REFERENCES "Account"("Id")         ON DELETE SET NULL,
	"DebitAmount"         REAL,
	"DebitAssetId"        INTEGER REFERENCES "Asset"("Id")           ON DELETE RESTRICT,
	"DebitCleared"        INTEGER NOT NULL DEFAULT(0),

	"RelatedAssetId"      INTEGER REFERENCES "Asset"("Id") ON DELETE CASCADE,

	"ParentTransactionId" INTEGER REFERENCES "Transaction_new"("Id") ON DELETE CASCADE
);

INSERT INTO "Transaction_new"
([Id], [When], [Action], [CheckNumber], [Memo], [Payee], [CategoryId], [CreditAccountId], [CreditAmount], [CreditAssetId], [CreditCleared], [ParentTransactionId])
SELECT [Id], [When], 1 AS [Action], [CheckNumber], [Memo], [Payee], [CategoryId], [CreditAccountId], [Amount] AS [CreditAmount], 1 AS [CreditAssetId], [Cleared] AS [CreditCleared], [ParentTransactionId]
FROM [Transaction]
WHERE [CreditAccountId] IS NOT NULL AND [DebitAccountId] IS NULL;

INSERT INTO "Transaction_new"
([Id], [When], [Action], [CheckNumber], [Memo], [Payee], [CategoryId], [DebitAccountId], [DebitAmount], [DebitAssetId], [DebitCleared], [ParentTransactionId])
SELECT [Id], [When], 2 AS [Action], [CheckNumber], [Memo], [Payee], [CategoryId], [DebitAccountId], [Amount] AS [DebitAmount], 1 AS [DebitAssetId], [Cleared] AS [DebitCleared], [ParentTransactionId]
FROM [Transaction]
WHERE [CreditAccountId] IS NULL AND [DebitAccountId] IS NOT NULL;

INSERT INTO "Transaction_new"
SELECT [Id], [When], 3 AS [Action], [CheckNumber], [Memo], [Payee], [CategoryId], [CreditAccountId], [Amount] AS [CreditAmount], 1 AS [CreditAssetId], [Cleared] AS [CreditCleared], [DebitAccountId], [Amount] AS [DebitAmount], 1 AS [DebitAssetId], [Cleared] AS [DebitCleared], NULL AS [RelatedAssetId], [ParentTransactionId]
FROM [Transaction]
WHERE [CreditAccountId] IS NOT NULL AND [DebitAccountId] IS NOT NULL;

DROP TABLE "Transaction";

ALTER TABLE "Transaction_new" RENAME TO "Transaction";

PRAGMA foreign_keys = on;

CREATE INDEX "Transaction_CreditAccountId" ON [Transaction]("CreditAccountId");
CREATE INDEX "Transaction_DebitAccountId" ON [Transaction]("DebitAccountId");
CREATE INDEX "Transaction_CreditAssetId" ON [Transaction]("CreditAssetId");
CREATE INDEX "Transaction_DebitAssetId" ON [Transaction]("DebitAssetId");
CREATE INDEX "Transaction_CategoryId" ON [Transaction]("CategoryId");
CREATE INDEX "Transaction_ParentTransactionId" ON [Transaction]("ParentTransactionId");

CREATE TABLE "AssetPrice" (
	"Id"                    INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
	"AssetId"               INTEGER          REFERENCES "Asset"("Id")  ON DELETE CASCADE,
	"When"                  INTEGER NOT NULL,
	"ReferenceAssetId"      INTEGER NOT NULL REFERENCES "Asset"("Id")  ON DELETE CASCADE,
	"PriceInReferenceAsset" REAL    NOT NULL
);

CREATE UNIQUE INDEX "AssetPrice_AssetId" ON [AssetPrice]("AssetId", "When" DESC);
