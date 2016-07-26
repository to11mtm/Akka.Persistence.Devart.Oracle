# Akka.Persistence.Devart.Oracle
Akka.Persistence.Devart.Oracle provider

--This release is for Akka 1.1.x. For Akka 1.0.x, please look at the other available branches.

This an implementation of an Oracle-compatible Akka.Persistence provider.

Journaling:

The Journal uses a table defined by your HOCON. The Table Can be created Automatically via setting 'auto-initialize' to 'on'.

Example HOCON Snippet for Journal. Replace {TABLE_NAME}, {METADATA_TABLE_NAME}, and {SCHEMA_NAME} with your desired table/schema as desired. Please also note that while the column names are reconfigurable, tables are fairly standardized across DBs with the column names used and thus it is not recommended to change them.

If you wish to create your own table(s), perhaps with a cycling (i.e. wraps on overflow) secondary sequence to track number of inserts, note the table must be set up as follows to allow for everything to work as expected:

-PersistenceId (for either table) should be VARCHAR2(255) or greater, NOT NULL

-SequenceNr (for either table) should be NUMBER(18), NOT NULL

-IsDeleted should be NUMBER(1), NOT NULL

-Manifest should be VARCHAR2(255) or greater, NOT NULL

-Timestamp should be NUMBER(18), NOT NULL. The timestamp itself is in tics (100 ns) since January 1, Gregorian year 0001, (0:00:00 UTC)

-Payload should be a BLOB, NOT NULL. Typically payloads are expected to be small (typ max 100k, typ under 50k)

-Tags should be a VARCHAR2(2000) or greater, NULL

-Primary Key should be the Persistence Id and Sequence Number for both tables. There should be no other unique indexes/foreign keys that could cause a failure.

-Queries are typically only on the primary key. Some usages may benefit from indexes on timestamp and manifest (alongside persistenceId) but in most cases the usage is limited enough to not warrant inclusion.

```
                    akka.persistence {
                        publish-plugin-commands = on
                        journal {
                            plugin = ""akka.persistence.journal.devart-oracle""
                            devart-oracle {
                                class = ""Akka.Persistence.Devart.Oracle.Journal.OracleJournal, Akka.Persistence.Devart.Oracle""
                                plugin-dispatcher = ""akka.actor.default-dispatcher""
								
								# default SQL commands timeout
								connection-timeout = 30s
								
								#Table Config. Either make sure these exist or that auto-initialize is set to on.
								table-name = {TABLE_NAME}
								metadata-table-name {METADATA_TABLE_NAME}
                                schema-name = {SCHEMA_NAME}
								
								#Automagically create tables on a best-effort basis if set to on
                                auto-initialize = on
								
								# NOTE: use either connection-string-name to get a connection string from config, or use connection-string directly. Either way one of these should be uncommented or injected at runtime.
                                # connection-string-name = ""TestDb""
								# connection-string = ""
								
								# These correspond to the columns created for the metadata and journal tables.
								# You only need to change these if you don't like the column names. Keep in mind migrating isn't a good thing to do midstream.
								# persistenceid-col-name = "persistenceid"
								# sequencenr-col-name = "sequencenr"
								# payload-col-name = "payload"
								# manifest-col-name = "manifest"
								# timestamp-col-name = "timestamp"
								# isdeleted-col-name = "isdeleted"
								# tags-col-name = "tags"
                            }
                        }
                    }
```
SnapshotStore:

The SnapshotStore uses a table defined by your HOCON. 

Example HOCON Snippet for SnapshotStore. Replace {TABLE_NAME} and {SCHEMA_NAME} with your desired table/schema as desired.Please also note that while the column names are reconfigurable, tables are fairly standardized across DBs with the column names used and thus it is not recommended to change them.

If you wish to create your own table(s), perhaps with a cycling (i.e. wraps on overflow) secondary sequence to track number of inserts, note the table must be set up as follows to allow for everything to work as expected:

-PersistenceId should be VARCHAR2(255) or greater, NOT NULL

-SequenceNr should be NUMBER(18), NOT NULL

-Manifest should be VARCHAR2(255) or greater, NOT NULL

-Timestamp should be NUMBER(18), NOT NULL. The timestamp itself is in tics (100 ns) since January 1, Gregorian year 0001, (0:00:00 UTC)

-Payload should be a BLOB, NOT NULL. Typically payloads are expected to be small (typ max 100k, typ under 50k)

-Primary Key should be the Persistence Id and Sequence Number. There should be no other unique indexes/foreign keys that could cause a failure on insert or update.

-Queries are typically only on the primary key. Some usages may benefit from indexes on timestamp (alongside persistenceId) but in most cases the usage is limited enough to not warrant inclusion.
```
                    akka.persistence {
                            publish-plugin-commands = on
                            snapshot-store {
                                plugin = ""akka.persistence.snapshot-store.devart-oracle""
                                devart-oracle {
                                    class = ""Akka.Persistence.Devart.Oracle.Snapshot.OracleSnapshotStore, Akka.Persistence.Devart.Oracle""
                                    plugin-dispatcher = ""akka.actor.default-dispatcher""
                                    table-name = {TABLE_NAME}
                                    schema-name = {SCHEMA_NAME}
									
									# default SQL commands timeout
									connection-timeout = 30s
                                    
									# Automagically create table on a best-effort basis if set to on
									auto-initialize = on
                                    
									# NOTE: use either connection-string-name to get a connection string from config, or use connection-string directly
                                    # connection-string-name = ""TestDb""
								    # connection-string = ""
									
									# These correspond to the columns created for the metadata and journal tables.
									# You only need to change these if you don't like the column names. Keep in mind migrating isn't a 	good thing to do midstream.
									persistenceid-col-name = "persistenceid"
            						sequencenr-col-name = "sequencenr"
									payload-col-name = "payload"
									manifest-col-name = "manifest"
									timestamp-col-name = "timestamp"
                                }
                            }
                        }
```