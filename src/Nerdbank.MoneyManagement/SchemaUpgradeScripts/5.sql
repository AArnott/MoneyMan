CREATE TABLE "AssetPrice" (
	"Id"                    INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
	"AssetId"               INTEGER          REFERENCES "Asset"("Id")  ON DELETE CASCADE,
	"When"                  INTEGER NOT NULL,
	"ReferenceAssetId"      INTEGER NOT NULL REFERENCES "Asset"("Id")  ON DELETE CASCADE,
	"PriceInReferenceAsset" REAL    NOT NULL
);

CREATE UNIQUE INDEX "AssetPrice_AssetId" ON [AssetPrice]("AssetId", "When" DESC);
