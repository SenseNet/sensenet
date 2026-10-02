------------------------------------------------------------
-- sensenet PostgreSQL Schema Installation Script
------------------------------------------------------------

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS citext;

------------------------------------------------------------
-- DROP EXISTING TABLES (reverse dependency order)
------------------------------------------------------------
DROP TABLE IF EXISTS "SharedLocks" CASCADE;
DROP TABLE IF EXISTS "AccessTokens" CASCADE;
DROP TABLE IF EXISTS "TreeLocks" CASCADE;
DROP TABLE IF EXISTS "Packages" CASCADE;
DROP TABLE IF EXISTS "SchemaModification" CASCADE;
DROP TABLE IF EXISTS "WorkflowNotification" CASCADE;
DROP TABLE IF EXISTS "IndexingActivities" CASCADE;
DROP TABLE IF EXISTS "LogEntries" CASCADE;
DROP TABLE IF EXISTS "JournalItems" CASCADE;
DROP TABLE IF EXISTS "ReferenceProperties" CASCADE;
DROP TABLE IF EXISTS "BinaryProperties" CASCADE;
DROP TABLE IF EXISTS "Files" CASCADE;
DROP TABLE IF EXISTS "LongTextProperties" CASCADE;
DROP TABLE IF EXISTS "Versions" CASCADE;
DROP TABLE IF EXISTS "Nodes" CASCADE;
DROP TABLE IF EXISTS "PropertyTypes" CASCADE;
DROP TABLE IF EXISTS "ContentListTypes" CASCADE;
DROP TABLE IF EXISTS "NodeTypes" CASCADE;

-- Drop existing views
DROP VIEW IF EXISTS "NodeInfoView";
DROP VIEW IF EXISTS "ReferencesInfoView";
DROP VIEW IF EXISTS "PermissionInfoView";
DROP VIEW IF EXISTS "MembershipInfoView";

------------------------------------------------------------
-- Timestamp trigger function (emulates MSSQL rowversion)
------------------------------------------------------------
CREATE OR REPLACE FUNCTION update_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW."Timestamp" := COALESCE(OLD."Timestamp", 0) + 1;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

------------------------------------------------------------
-- CREATE TABLES
------------------------------------------------------------

-- NodeTypes
CREATE TABLE IF NOT EXISTS "NodeTypes" (
    "NodeTypeId" SERIAL PRIMARY KEY,
    "ParentId" INT NULL,
    "Name" VARCHAR(450) NOT NULL,
    "ClassName" VARCHAR(450) NULL,
    "Properties" TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS "ix_nodetypes_parentid" ON "NodeTypes" ("ParentId");
CREATE INDEX IF NOT EXISTS "ix_nodetypes_name" ON "NodeTypes" ("Name") INCLUDE ("NodeTypeId");

-- ContentListTypes
CREATE TABLE IF NOT EXISTS "ContentListTypes" (
    "ContentListTypeId" SERIAL PRIMARY KEY,
    "Name" VARCHAR(450) NOT NULL,
    "Properties" TEXT NOT NULL
);

-- PropertyTypes
CREATE TABLE IF NOT EXISTS "PropertyTypes" (
    "PropertyTypeId" SERIAL PRIMARY KEY,
    "Name" VARCHAR(450) NOT NULL,
    "DataType" VARCHAR(10) NOT NULL,
    "Mapping" INT NOT NULL,
    "IsContentListProperty" SMALLINT NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS "ix_propertytypes_name" ON "PropertyTypes" ("Name") INCLUDE ("PropertyTypeId");

-- Nodes
CREATE TABLE IF NOT EXISTS "Nodes" (
    "NodeId" SERIAL PRIMARY KEY,
    "NodeTypeId" INT NOT NULL,
    "ContentListTypeId" INT NULL,
    "ContentListId" INT NULL,
    "CreatingInProgress" SMALLINT NOT NULL DEFAULT 0,
    "IsDeleted" SMALLINT NOT NULL,
    "IsInherited" SMALLINT NOT NULL DEFAULT 1,
    "ParentNodeId" INT NULL,
    "Name" VARCHAR(450) NOT NULL,
    "Path" CITEXT NOT NULL,
    "Index" INT NOT NULL,
    "Locked" SMALLINT NOT NULL,
    "LockedById" INT NULL,
    "ETag" VARCHAR(50) NOT NULL,
    "LockType" INT NOT NULL,
    "LockTimeout" INT NOT NULL,
    "LockDate" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "LockToken" VARCHAR(50) NOT NULL,
    "LastLockUpdate" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "LastMinorVersionId" INT NULL,
    "LastMajorVersionId" INT NULL,
    "CreationDate" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "CreatedById" INT NOT NULL,
    "ModificationDate" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "ModifiedById" INT NOT NULL,
    "DisplayName" VARCHAR(450) NULL,
    "IsSystem" SMALLINT NULL,
    "OwnerId" INT NOT NULL,
    "SavingState" INT NULL,
    "RowGuid" UUID NOT NULL DEFAULT gen_random_uuid(),
    "Timestamp" BIGINT NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Nodes_Path" ON "Nodes" ("Path") INCLUDE ("NodeId");
CREATE INDEX IF NOT EXISTS "IX_Nodes_ParentNodeId" ON "Nodes" ("ParentNodeId");
CREATE INDEX IF NOT EXISTS "IX_Nodes_NodeTypeId" ON "Nodes" ("NodeTypeId");

CREATE TRIGGER trg_update_timestamp_nodes
    BEFORE UPDATE ON "Nodes"
    FOR EACH ROW
    EXECUTE FUNCTION update_timestamp();

-- Versions
CREATE TABLE IF NOT EXISTS "Versions" (
    "VersionId" SERIAL PRIMARY KEY,
    "NodeId" INT NOT NULL,
    "MajorNumber" SMALLINT NOT NULL,
    "MinorNumber" SMALLINT NOT NULL,
    "CreationDate" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "CreatedById" INT NOT NULL,
    "ModificationDate" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "ModifiedById" INT NOT NULL,
    "Status" SMALLINT NOT NULL DEFAULT 1,
    "IndexDocument" TEXT NULL,
    "ChangedData" TEXT NULL,
    "DynamicProperties" TEXT NULL,
    "ContentListProperties" TEXT NULL,
    "RowGuid" UUID NOT NULL DEFAULT gen_random_uuid(),
    "Timestamp" BIGINT NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS "ix_Versions_NodeId" ON "Versions" ("NodeId");
CREATE INDEX IF NOT EXISTS "ix_Versions_NodeId_MinorNumber_MajorNumber_Status"
    ON "Versions" ("NodeId", "MinorNumber", "Status");

CREATE TRIGGER trg_update_timestamp_versions
    BEFORE UPDATE ON "Versions"
    FOR EACH ROW
    EXECUTE FUNCTION update_timestamp();

-- Files
CREATE TABLE IF NOT EXISTS "Files" (
    "FileId" SERIAL PRIMARY KEY,
    "ContentType" VARCHAR(450) NOT NULL,
    "FileNameWithoutExtension" VARCHAR(450) NULL,
    "Extension" VARCHAR(50) NOT NULL,
    "Size" BIGINT NOT NULL,
    "Checksum" VARCHAR(200) NULL,
    "Stream" BYTEA NULL,
    "CreationDate" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    "RowGuid" UUID NOT NULL UNIQUE DEFAULT gen_random_uuid(),
    "Timestamp" BIGINT NOT NULL DEFAULT 0,
    "Staging" BOOLEAN NULL,
    "StagingVersionId" INT NULL,
    "StagingPropertyTypeId" INT NULL,
    "IsDeleted" BOOLEAN NULL,
    "BlobProvider" VARCHAR(450) NULL,
    "BlobProviderData" TEXT NULL
);

CREATE TRIGGER trg_update_timestamp_files
    BEFORE UPDATE ON "Files"
    FOR EACH ROW
    EXECUTE FUNCTION update_timestamp();

-- BinaryProperties
CREATE TABLE IF NOT EXISTS "BinaryProperties" (
    "BinaryPropertyId" SERIAL PRIMARY KEY,
    "VersionId" INT NULL,
    "PropertyTypeId" INT NULL,
    "FileId" INT NOT NULL
);

CREATE INDEX IF NOT EXISTS "ix_binaryproperties_version_id" ON "BinaryProperties" ("VersionId");
CREATE INDEX IF NOT EXISTS "ix_binaryproperties_file_id" ON "BinaryProperties" ("FileId");

-- ReferenceProperties
CREATE TABLE IF NOT EXISTS "ReferenceProperties" (
    "ReferencePropertyId" SERIAL PRIMARY KEY,
    "VersionId" INT NOT NULL,
    "PropertyTypeId" INT NOT NULL,
    "ReferredNodeId" INT NOT NULL
);

CREATE INDEX IF NOT EXISTS "IX_ReferenceProperties_VersionIdPropertyTypeId"
    ON "ReferenceProperties" ("VersionId", "PropertyTypeId");
CREATE INDEX IF NOT EXISTS "IX_ReferenceProperties_ReferredNodeId"
    ON "ReferenceProperties" ("ReferredNodeId");
CREATE INDEX IF NOT EXISTS "ix_referenceproperties_version_id" ON "ReferenceProperties" ("VersionId");

-- LongTextProperties
CREATE TABLE IF NOT EXISTS "LongTextProperties" (
    "LongTextPropertyId" SERIAL PRIMARY KEY,
    "VersionId" INT NOT NULL,
    "PropertyTypeId" INT NOT NULL,
    "Length" INT NULL,
    "Value" TEXT NULL
);

CREATE INDEX IF NOT EXISTS "ix_longtextproperties_version_id" ON "LongTextProperties" ("VersionId");

------------------------------------------------------------
-- CREATE VIEWS
------------------------------------------------------------

CREATE OR REPLACE VIEW "NodeInfoView" AS
SELECT N."NodeId", T."Name" AS "Type", N."Name", N."Path"::TEXT AS "Path", N."LockedById",
       V."VersionId",
       CAST(V."MajorNumber" AS VARCHAR) || '.' || CAST(V."MinorNumber" AS VARCHAR) || '.' ||
       CASE V."Status"
           WHEN 1 THEN 'A' WHEN 2 THEN 'L' WHEN 4 THEN 'D'
           WHEN 8 THEN 'R' WHEN 16 THEN 'P' ELSE '' END AS "Version",
       CASE V."VersionId" WHEN N."LastMajorVersionId" THEN 'TRUE' ELSE 'false' END AS "LastPub",
       CASE V."VersionId" WHEN N."LastMinorVersionId" THEN 'TRUE' ELSE 'false' END AS "LastWork"
FROM "Versions" AS V
    INNER JOIN "Nodes" AS N ON V."NodeId" = N."NodeId"
    INNER JOIN "NodeTypes" AS T ON N."NodeTypeId" = T."NodeTypeId";

CREATE OR REPLACE VIEW "ReferencesInfoView" AS
SELECT Nodes."Name" AS "SrcName",
       'V' || CAST(Versions."MajorNumber" AS VARCHAR) || '.' || CAST(Versions."MinorNumber" AS VARCHAR) AS "SrcVer",
       Slots."Name" AS "RelType", RefNodes."Name" AS "TargetName",
       Nodes."NodeId" AS "SrcId", RefNodes."NodeId" AS "TargetId",
       Nodes."Path"::TEXT AS "SrcPath", RefNodes."Path"::TEXT AS "TargetPath"
FROM "ReferenceProperties" AS Refs
    INNER JOIN "Versions" AS Versions ON Refs."VersionId" = Versions."VersionId"
    INNER JOIN "Nodes" AS Nodes ON Versions."NodeId" = Nodes."NodeId"
    INNER JOIN "Nodes" AS RefNodes ON Refs."ReferredNodeId" = RefNodes."NodeId"
    INNER JOIN "PropertyTypes" AS Slots ON Refs."PropertyTypeId" = Slots."PropertyTypeId"
UNION ALL
SELECT Nodes."Name", 'V*.*', 'Parent', RefNodes."Name",
       Nodes."NodeId", RefNodes."NodeId", Nodes."Path"::TEXT, RefNodes."Path"::TEXT
FROM "Nodes" AS Nodes
    INNER JOIN "Nodes" AS RefNodes ON Nodes."ParentNodeId" = RefNodes."NodeId"
UNION ALL
SELECT Nodes."Name", 'V*.*', 'LockedById', RefNodes."Name",
       Nodes."NodeId", RefNodes."NodeId", Nodes."Path"::TEXT, RefNodes."Path"::TEXT
FROM "Nodes" AS Nodes
    INNER JOIN "Nodes" AS RefNodes ON Nodes."LockedById" = RefNodes."NodeId"
UNION ALL
SELECT Nodes."Name",
       'V' || CAST(Versions."MajorNumber" AS VARCHAR) || '.' || CAST(Versions."MinorNumber" AS VARCHAR),
       'CreatedById', RefNodes."Name",
       Nodes."NodeId", RefNodes."NodeId", Nodes."Path"::TEXT, RefNodes."Path"::TEXT
FROM "Nodes" AS Nodes
    INNER JOIN "Versions" AS Versions ON Nodes."NodeId" = Versions."NodeId"
    INNER JOIN "Nodes" AS RefNodes ON Versions."CreatedById" = RefNodes."NodeId"
UNION ALL
SELECT Nodes."Name",
       'V' || CAST(Versions."MajorNumber" AS VARCHAR) || '.' || CAST(Versions."MinorNumber" AS VARCHAR),
       'ModifiedById', RefNodes."Name",
       Nodes."NodeId", RefNodes."NodeId", Nodes."Path"::TEXT, RefNodes."Path"::TEXT
FROM "Nodes" AS Nodes
    INNER JOIN "Versions" AS Versions ON Nodes."NodeId" = Versions."NodeId"
    INNER JOIN "Nodes" AS RefNodes ON Versions."ModifiedById" = RefNodes."NodeId";

------------------------------------------------------------
-- ADDITIONAL TABLES
------------------------------------------------------------

-- JournalItems
CREATE TABLE IF NOT EXISTS "JournalItems" (
    "Id" SERIAL PRIMARY KEY,
    "When" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "Wherewith" VARCHAR(450) NOT NULL,
    "What" VARCHAR(100) NOT NULL,
    "Who" VARCHAR(200) NOT NULL,
    "RowGuid" UUID NOT NULL DEFAULT gen_random_uuid(),
    "Timestamp" BIGINT NOT NULL DEFAULT 0,
    "NodeId" INT NOT NULL,
    "DisplayName" VARCHAR(450) NOT NULL,
    "NodeTypeName" VARCHAR(100) NOT NULL,
    "SourcePath" VARCHAR(450) NULL,
    "TargetPath" VARCHAR(450) NULL,
    "TargetDisplayName" VARCHAR(450) NULL,
    "Hidden" BOOLEAN NOT NULL,
    "Details" VARCHAR(450) NULL
);

CREATE INDEX IF NOT EXISTS "IX_JournalItems" ON "JournalItems" ("When" DESC, "Wherewith");

CREATE TRIGGER trg_update_timestamp_journalitems
    BEFORE UPDATE ON "JournalItems"
    FOR EACH ROW
    EXECUTE FUNCTION update_timestamp();

-- LogEntries
CREATE TABLE IF NOT EXISTS "LogEntries" (
    "LogId" SERIAL PRIMARY KEY,
    "EventId" INT NOT NULL,
    "Category" VARCHAR(50) NULL,
    "Priority" INT NOT NULL,
    "Severity" VARCHAR(30) NOT NULL,
    "Title" VARCHAR(256) NULL,
    "ContentId" INT NULL,
    "ContentPath" VARCHAR(450) NULL,
    "UserName" VARCHAR(450) NULL,
    "LogDate" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "MachineName" VARCHAR(32) NULL,
    "AppDomainName" VARCHAR(512) NULL,
    "ProcessID" VARCHAR(256) NULL,
    "ProcessName" VARCHAR(512) NULL,
    "ThreadName" VARCHAR(512) NULL,
    "Win32ThreadId" VARCHAR(128) NULL,
    "Message" VARCHAR(1500) NULL,
    "FormattedMessage" TEXT NULL,
    "RowGuid" UUID NOT NULL DEFAULT gen_random_uuid(),
    "Timestamp" BIGINT NOT NULL DEFAULT 0
);

CREATE TRIGGER trg_update_timestamp_logentries
    BEFORE UPDATE ON "LogEntries"
    FOR EACH ROW
    EXECUTE FUNCTION update_timestamp();

-- IndexingActivities
CREATE TABLE IF NOT EXISTS "IndexingActivities" (
    "IndexingActivityId" SERIAL PRIMARY KEY,
    "ActivityType" VARCHAR(50) NOT NULL,
    "CreationDate" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "RunningState" VARCHAR(10) NOT NULL,
    "LockTime" TIMESTAMP WITHOUT TIME ZONE NULL,
    "NodeId" INT NOT NULL,
    "VersionId" INT NOT NULL,
    "Path" VARCHAR(450) NOT NULL,
    "VersionTimestamp" BIGINT NULL,
    "Extension" TEXT NULL
);

-- WorkflowNotification
CREATE TABLE IF NOT EXISTS "WorkflowNotification" (
    "NotificationId" SERIAL PRIMARY KEY,
    "NodeId" INT NOT NULL,
    "WorkflowInstanceId" UUID NOT NULL,
    "WorkflowNodePath" VARCHAR(450) NOT NULL,
    "BookmarkName" VARCHAR(50) NOT NULL
);

-- SchemaModification
CREATE TABLE IF NOT EXISTS "SchemaModification" (
    "SchemaModificationId" SERIAL PRIMARY KEY,
    "ModificationDate" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "LockToken" VARCHAR(50) NULL,
    "Timestamp" BIGINT NOT NULL DEFAULT 0
);

CREATE TRIGGER trg_update_timestamp_schemamodification
    BEFORE UPDATE ON "SchemaModification"
    FOR EACH ROW
    EXECUTE FUNCTION update_timestamp();

-- Packages
CREATE TABLE IF NOT EXISTS "Packages" (
    "Id" SERIAL PRIMARY KEY,
    "PackageType" VARCHAR(50) NOT NULL,
    "ComponentId" VARCHAR(450) NULL,
    "ComponentVersion" VARCHAR(50) NULL,
    "ReleaseDate" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "ExecutionDate" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "ExecutionResult" VARCHAR(50) NOT NULL,
    "ExecutionError" TEXT NULL,
    "Description" VARCHAR(1000) NULL,
    "Manifest" TEXT NULL
);

-- TreeLocks
CREATE TABLE IF NOT EXISTS "TreeLocks" (
    "TreeLockId" SERIAL PRIMARY KEY,
    "Path" VARCHAR(450) NOT NULL,
    "LockedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL
);

-- AccessTokens
CREATE TABLE IF NOT EXISTS "AccessTokens" (
    "AccessTokenId" SERIAL PRIMARY KEY,
    "Value" VARCHAR(1000) NOT NULL,
    "UserId" INT NOT NULL,
    "ContentId" INT NULL,
    "Feature" VARCHAR(1000) NULL,
    "CreationDate" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "ExpirationDate" TIMESTAMP WITHOUT TIME ZONE NOT NULL
);

-- SharedLocks
CREATE TABLE IF NOT EXISTS "SharedLocks" (
    "SharedLockId" SERIAL PRIMARY KEY,
    "ContentId" INT NOT NULL,
    "Lock" VARCHAR(1000) NOT NULL,
    "CreationDate" TIMESTAMP WITHOUT TIME ZONE NOT NULL
);

------------------------------------------------------------
-- STORED FUNCTIONS
------------------------------------------------------------

-- Function for RestoreIndexingActivityStatus
CREATE OR REPLACE FUNCTION sn_restore_indexing_activity_status(p_last_activity_id INT, p_gaps TEXT)
RETURNS TEXT AS $$
DECLARE
    _state_string TEXT;
    _last_in_db INT;
    _gaps_in_db TEXT;
BEGIN
    -- Lock the table to prevent concurrent modifications
    LOCK TABLE "IndexingActivities" IN EXCLUSIVE MODE;

    _state_string := p_last_activity_id::TEXT || '(' || COALESCE(p_gaps, '') || ')';

    -- Check if already restored with same state
    IF EXISTS (SELECT 1 FROM "IndexingActivities"
               WHERE "ActivityType" = 'Restore' AND "Extension" = _state_string) THEN
        RETURN 'AlreadyRestored';
    END IF;

    -- Get current state from DB
    SELECT "IndexingActivityId" INTO _last_in_db
    FROM "IndexingActivities"
    WHERE "RunningState" = 'Done'
    ORDER BY "CreationDate" DESC
    LIMIT 1;

    IF _last_in_db IS NULL THEN
        _last_in_db := 0;
    END IF;

    SELECT string_agg("IndexingActivityId"::TEXT, ',' ORDER BY "IndexingActivityId")
    INTO _gaps_in_db
    FROM "IndexingActivities"
    WHERE "RunningState" != 'Done' AND "IndexingActivityId" < _last_in_db;

    -- Check if restoration is necessary
    IF p_last_activity_id = _last_in_db AND COALESCE(p_gaps, '') = COALESCE(_gaps_in_db, '') THEN
        RETURN 'NotNecessary';
    END IF;

    -- Perform the restoration
    -- Reset activities after the restore point, and gap activities, to 'Waiting'
    UPDATE "IndexingActivities" SET "RunningState" = 'Waiting'
    WHERE "IndexingActivityId" > p_last_activity_id
       OR "IndexingActivityId" = ANY(string_to_array(p_gaps, ',')::int[]);

    -- Record the restore operation
    INSERT INTO "IndexingActivities"
        ("ActivityType", "CreationDate", "RunningState", "LockTime",
         "NodeId", "VersionId", "Path", "VersionTimestamp", "Extension")
    VALUES
        ('Restore', NOW() AT TIME ZONE 'UTC', 'Done', NOW() AT TIME ZONE 'UTC',
         0, 0, '', 0, _state_string);

    RETURN 'Restored';
END;
$$ LANGUAGE plpgsql;

------------------------------------------------------------
-- Function: sn_insert_node_and_version
-- Atomically inserts a Node, a Version, and updates the
-- Node's LastMinorVersionId and LastMajorVersionId.
-- Returns the new IDs and timestamps.
------------------------------------------------------------
CREATE OR REPLACE FUNCTION sn_insert_node_and_version(
    p_node_type_id INT,
    p_content_list_type_id INT,
    p_content_list_id INT,
    p_creating_in_progress SMALLINT,
    p_is_deleted SMALLINT,
    p_is_inherited SMALLINT,
    p_parent_node_id INT,
    p_name VARCHAR(450),
    p_display_name VARCHAR(450),
    p_path VARCHAR(450),
    p_index INT,
    p_locked SMALLINT,
    p_locked_by_id INT,
    p_etag VARCHAR(50),
    p_lock_type INT,
    p_lock_timeout INT,
    p_lock_date TIMESTAMP WITHOUT TIME ZONE,
    p_lock_token VARCHAR(50),
    p_last_lock_update TIMESTAMP WITHOUT TIME ZONE,
    p_creation_date TIMESTAMP WITHOUT TIME ZONE,
    p_created_by_id INT,
    p_modification_date TIMESTAMP WITHOUT TIME ZONE,
    p_modified_by_id INT,
    p_is_system SMALLINT,
    p_owner_id INT,
    p_saving_state INT,
    p_major_number SMALLINT,
    p_minor_number SMALLINT,
    p_status SMALLINT,
    p_changed_data TEXT,
    p_version_creation_date TIMESTAMP WITHOUT TIME ZONE,
    p_version_created_by_id INT,
    p_version_modification_date TIMESTAMP WITHOUT TIME ZONE,
    p_version_modified_by_id INT,
    p_dynamic_properties TEXT,
    p_content_list_properties TEXT
)
RETURNS TABLE(
    "NodeId" INT,
    "NodeTimestamp" BIGINT,
    "VersionId" INT,
    "VersionTimestamp" BIGINT,
    "LastMajorVersionId" INT,
    "LastMinorVersionId" INT,
    "Path" TEXT
) AS $$
DECLARE
    v_node_id INT;
    v_version_id INT;
    v_node_ts BIGINT;
    v_version_ts BIGINT;
BEGIN
    INSERT INTO "Nodes"
        ("NodeTypeId", "ContentListTypeId", "ContentListId", "CreatingInProgress", "IsDeleted", "IsInherited",
         "ParentNodeId", "Name", "DisplayName", "Path", "Index", "Locked", "LockedById",
         "ETag", "LockType", "LockTimeout", "LockDate", "LockToken", "LastLockUpdate",
         "CreationDate", "CreatedById", "ModificationDate", "ModifiedById",
         "IsSystem", "OwnerId", "SavingState")
    VALUES
        (p_node_type_id, p_content_list_type_id, p_content_list_id, p_creating_in_progress, p_is_deleted, p_is_inherited,
         p_parent_node_id, p_name, p_display_name, p_path, p_index, p_locked, p_locked_by_id,
         p_etag, p_lock_type, p_lock_timeout, p_lock_date, p_lock_token, p_last_lock_update,
         p_creation_date, p_created_by_id, p_modification_date, p_modified_by_id,
         p_is_system, p_owner_id, p_saving_state)
    RETURNING "Nodes"."NodeId", "Nodes"."Timestamp" INTO v_node_id, v_node_ts;

    INSERT INTO "Versions"
        ("NodeId", "MajorNumber", "MinorNumber", "CreationDate", "CreatedById",
         "ModificationDate", "ModifiedById", "Status", "ChangedData",
         "DynamicProperties", "ContentListProperties")
    VALUES
        (v_node_id, p_major_number, p_minor_number, p_version_creation_date, p_version_created_by_id,
         p_version_modification_date, p_version_modified_by_id, p_status, p_changed_data,
         p_dynamic_properties, p_content_list_properties)
    RETURNING "Versions"."VersionId", "Versions"."Timestamp" INTO v_version_id, v_version_ts;

    IF p_status = 1 THEN
        UPDATE "Nodes" SET "LastMinorVersionId" = v_version_id, "LastMajorVersionId" = v_version_id
        WHERE "Nodes"."NodeId" = v_node_id;
    ELSE
        UPDATE "Nodes" SET "LastMinorVersionId" = v_version_id
        WHERE "Nodes"."NodeId" = v_node_id;
    END IF;

    SELECT n."Timestamp" INTO v_node_ts FROM "Nodes" n WHERE n."NodeId" = v_node_id;

    RETURN QUERY
    SELECT v_node_id, v_node_ts, v_version_id, v_version_ts,
           n."LastMajorVersionId", n."LastMinorVersionId", n."Path"::TEXT
    FROM "Nodes" n
    WHERE n."NodeId" = v_node_id;
END;
$$ LANGUAGE plpgsql;

------------------------------------------------------------
-- FOREIGN KEY CONSTRAINTS
------------------------------------------------------------
ALTER TABLE "BinaryProperties" ADD CONSTRAINT "FK_BinaryProperties_PropertyTypes"
    FOREIGN KEY ("PropertyTypeId") REFERENCES "PropertyTypes" ("PropertyTypeId");
ALTER TABLE "BinaryProperties" ADD CONSTRAINT "FK_BinaryProperties_Versions"
    FOREIGN KEY ("VersionId") REFERENCES "Versions" ("VersionId");
ALTER TABLE "BinaryProperties" ADD CONSTRAINT "FK_BinaryProperties_Files"
    FOREIGN KEY ("FileId") REFERENCES "Files" ("FileId");

ALTER TABLE "Nodes" ADD CONSTRAINT "FK_Nodes_LockedBy"
    FOREIGN KEY ("LockedById") REFERENCES "Nodes" ("NodeId");
ALTER TABLE "Nodes" ADD CONSTRAINT "FK_Nodes_Parent"
    FOREIGN KEY ("ParentNodeId") REFERENCES "Nodes" ("NodeId");
ALTER TABLE "Nodes" ADD CONSTRAINT "FK_Nodes_NodeTypes"
    FOREIGN KEY ("NodeTypeId") REFERENCES "NodeTypes" ("NodeTypeId");
ALTER TABLE "Nodes" ADD CONSTRAINT "FK_Nodes_Nodes_CreatedById"
    FOREIGN KEY ("CreatedById") REFERENCES "Nodes" ("NodeId");
ALTER TABLE "Nodes" ADD CONSTRAINT "FK_Nodes_Nodes_ModifiedById"
    FOREIGN KEY ("ModifiedById") REFERENCES "Nodes" ("NodeId");
ALTER TABLE "Nodes" ADD CONSTRAINT "FK_Nodes_Nodes_ContentListId"
    FOREIGN KEY ("ContentListId") REFERENCES "Nodes" ("NodeId");

ALTER TABLE "ReferenceProperties" ADD CONSTRAINT "FK_ReferenceProperties_PropertyTypes"
    FOREIGN KEY ("PropertyTypeId") REFERENCES "PropertyTypes" ("PropertyTypeId");

ALTER TABLE "NodeTypes" ADD CONSTRAINT "FK_NodeTypes_NodeTypes"
    FOREIGN KEY ("ParentId") REFERENCES "NodeTypes" ("NodeTypeId");

ALTER TABLE "LongTextProperties" ADD CONSTRAINT "FK_LongTextProperties_PropertyTypes"
    FOREIGN KEY ("PropertyTypeId") REFERENCES "PropertyTypes" ("PropertyTypeId");
ALTER TABLE "LongTextProperties" ADD CONSTRAINT "FK_LongTextProperties_Versions"
    FOREIGN KEY ("VersionId") REFERENCES "Versions" ("VersionId");

ALTER TABLE "Versions" ADD CONSTRAINT "FK_Versions_Nodes"
    FOREIGN KEY ("NodeId") REFERENCES "Nodes" ("NodeId");
ALTER TABLE "Versions" ADD CONSTRAINT "FK_Versions_Nodes_CreatedBy"
    FOREIGN KEY ("CreatedById") REFERENCES "Nodes" ("NodeId");
ALTER TABLE "Versions" ADD CONSTRAINT "FK_Versions_Nodes_ModifiedBy"
    FOREIGN KEY ("ModifiedById") REFERENCES "Nodes" ("NodeId");

------------------------------------------------------------
-- Disable foreign keys for initial data loading
------------------------------------------------------------
ALTER TABLE "BinaryProperties" DISABLE TRIGGER ALL;
ALTER TABLE "Nodes" DISABLE TRIGGER ALL;
ALTER TABLE "ReferenceProperties" DISABLE TRIGGER ALL;
ALTER TABLE "LongTextProperties" DISABLE TRIGGER ALL;
ALTER TABLE "Versions" DISABLE TRIGGER ALL;

------------------------------------------------------------
-- StatisticalData tables
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "StatisticalData" (
    "Id" SERIAL PRIMARY KEY,
    "DataType" VARCHAR(50) NOT NULL,
    "CreationTime" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "WrittenTime" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "Duration" BIGINT NULL,
    "RequestLength" BIGINT NULL,
    "ResponseLength" BIGINT NULL,
    "ResponseStatusCode" INT NULL,
    "Url" VARCHAR(1000) NULL,
    "TargetId" INT NULL,
    "ContentId" INT NULL,
    "EventName" VARCHAR(50) NULL,
    "ErrorMessage" VARCHAR(500) NULL,
    "GeneralData" TEXT NULL
);

CREATE INDEX IF NOT EXISTS "IX_StatisticalData_DataType_CreationTime"
    ON "StatisticalData" ("DataType", "CreationTime");

CREATE TABLE IF NOT EXISTS "StatisticalAggregations" (
    "DataType" VARCHAR(50) NOT NULL,
    "Date" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "Resolution" VARCHAR(10) NOT NULL,
    "Data" TEXT NULL,
    CONSTRAINT "PK_StatisticalAggregations" PRIMARY KEY ("DataType", "Date", "Resolution")
);

------------------------------------------------------------
-- ExclusiveLock table
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "ExclusiveLocks" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(450) NOT NULL UNIQUE,
    "OperationId" VARCHAR(450) NOT NULL,
    "TimeLimit" TIMESTAMP WITHOUT TIME ZONE NOT NULL
);

------------------------------------------------------------
-- ClientStore tables
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "ClientApps" (
    "ClientId" VARCHAR(50) NOT NULL PRIMARY KEY,
    "Name" VARCHAR(450) NULL,
    "Repository" VARCHAR(450) NULL,
    "UserName" VARCHAR(450) NULL,
    "Authority" VARCHAR(450) NULL,
    "Type" INT NULL
);

CREATE INDEX IF NOT EXISTS "IX_ClientApps_Repository" ON "ClientApps" ("Repository");
CREATE INDEX IF NOT EXISTS "IX_ClientApps_Authority" ON "ClientApps" ("Authority");

CREATE TABLE IF NOT EXISTS "ClientSecrets" (
    "Id" VARCHAR(50) NOT NULL PRIMARY KEY,
    "ClientId" VARCHAR(50) NOT NULL REFERENCES "ClientApps" ("ClientId"),
    "Value" VARCHAR(450) NOT NULL,
    "CreationDate" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "ValidTill" TIMESTAMP WITHOUT TIME ZONE NOT NULL
);

CREATE INDEX IF NOT EXISTS "IX_ClientSecrets_ClientId" ON "ClientSecrets" ("ClientId");
