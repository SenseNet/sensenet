------------------------------------------------------------
-- sensenet PostgreSQL Security (EF) Installation Script
------------------------------------------------------------

DROP TABLE IF EXISTS "EFMessages" CASCADE;
DROP TABLE IF EXISTS "EFMemberships" CASCADE;
DROP TABLE IF EXISTS "EFEntries" CASCADE;
DROP TABLE IF EXISTS "EFEntities" CASCADE;

-- EFEntities
CREATE TABLE IF NOT EXISTS "EFEntities" (
    "Id" INT NOT NULL,
    "OwnerId" INT NULL,
    "ParentId" INT NULL,
    "IsInherited" BOOLEAN NOT NULL,
    CONSTRAINT "PK_EFEntities" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_EFEntities_ParentId" ON "EFEntities" ("ParentId");

-- EFEntries
CREATE TABLE IF NOT EXISTS "EFEntries" (
    "EFEntityId" INT NOT NULL,
    "EntryType" INT NOT NULL,
    "IdentityId" INT NOT NULL,
    "LocalOnly" BOOLEAN NOT NULL,
    "AllowBits" BIGINT NOT NULL,
    "DenyBits" BIGINT NOT NULL,
    CONSTRAINT "PK_EFEntries" PRIMARY KEY ("EFEntityId", "EntryType", "IdentityId", "LocalOnly")
);

CREATE INDEX IF NOT EXISTS "IX_EFEntries_EFEntityId" ON "EFEntries" ("EFEntityId");

-- EFMemberships
CREATE TABLE IF NOT EXISTS "EFMemberships" (
    "GroupId" INT NOT NULL,
    "MemberId" INT NOT NULL,
    "IsUser" BOOLEAN NOT NULL,
    CONSTRAINT "PK_EFMemberships" PRIMARY KEY ("GroupId", "MemberId")
);

-- EFMessages
CREATE TABLE IF NOT EXISTS "EFMessages" (
    "Id" SERIAL PRIMARY KEY,
    "SavedBy" TEXT NULL,
    "SavedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "ExecutionState" TEXT NULL,
    "LockedBy" TEXT NULL,
    "LockedAt" TIMESTAMP WITHOUT TIME ZONE NULL,
    "Body" BYTEA NULL
);

-- Foreign Keys
ALTER TABLE "EFEntities" ADD CONSTRAINT "FK_EFEntities_EFEntities_ParentId"
    FOREIGN KEY ("ParentId") REFERENCES "EFEntities" ("Id");
ALTER TABLE "EFEntries" ADD CONSTRAINT "FK_EFEntries_EFEntities_EFEntityId"
    FOREIGN KEY ("EFEntityId") REFERENCES "EFEntities" ("Id");
