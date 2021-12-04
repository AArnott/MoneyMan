CREATE TABLE "InvestingTransaction" (
	"Id"                  INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
	"When"                INTEGER NOT NULL,
	"Action"              INTEGER NOT NULL, -- InvestmentAction

	"CreditAccountId"     INTEGER REFERENCES "Account"("Id")     ON DELETE RESTRICT,
	"CreditAmount"        REAL,
	"CreditAssetId"       INTEGER REFERENCES "Asset"("Id")       ON DELETE RESTRICT,

	"DebitAccountId"      INTEGER REFERENCES "Account"("Id")     ON DELETE RESTRICT,
	"DebitAmount"         REAL,
	"DebitAssetId"        INTEGER REFERENCES "Asset"("Id")       ON DELETE RESTRICT,

	"FeeAccountId"        INTEGER REFERENCES "Account"("Id")     ON DELETE RESTRICT,
	"FeeAmount"           REAL,
	"FeeAssetId"          INTEGER REFERENCES "Asset"("Id")       ON DELETE RESTRICT,

	"ValueInCash"         REAL NOT NULL,

	"Cleared"             INTEGER NOT NULL DEFAULT(0)
)
