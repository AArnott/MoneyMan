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
);

CREATE INDEX "InvestingTransaction_CreditAccountId" on "InvestingTransaction"("CreditAccountId");
CREATE INDEX "InvestingTransaction_DebitAccountId" on "InvestingTransaction"("DebitAccountId");
CREATE INDEX "InvestingTransaction_FeeAccountId" on "InvestingTransaction"("FeeAccountId");
CREATE INDEX "InvestingTransaction_CreditAssetId" on "InvestingTransaction"("CreditAssetId");
CREATE INDEX "InvestingTransaction_DebitAssetId" on "InvestingTransaction"("DebitAssetId");
CREATE INDEX "InvestingTransaction_FeeAssetId" on "InvestingTransaction"("FeeAssetId");

ALTER TABLE "Asset"
	ADD "Type" INTEGER NOT NULL DEFAULT(1)
;

INSERT INTO "Asset" ("Name", "Type") VALUES("USD", 0);

ALTER TABLE "Account"
	-- Asset.Id of the currency used for this account if Type = 0 (Banking).
	ADD "CurrencyAssetId" INTEGER REFERENCES "Asset"("Id") ON DELETE RESTRICT
;
