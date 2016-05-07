# Akka.Persistence.Devart.Oracle
Akka.Persistence.Devart.Oracle provider

This an implementation of an Oracle-compatible Akka.Persistence provider.

Journaling:

The Journal uses a table defined by your HOCON. The Table to be used should be set up in a manner similar to what you see in CREATE_JOURNAL_TABLE.SQL

Example HOCON Snippet for Journal. Replace {TABLE_NAME} and {SCHEMA_NAME} with your desired table/schema as desired.
```
                    akka.persistence {
                        publish-plugin-commands = on
                        journal {
                            plugin = ""akka.persistence.journal.devart-oracle""
                            devart-oracle {
                                class = ""Akka.Persistence.Devart.Oracle.Journal.OracleJournal, Akka.Persistence.Devart.Oracle""
                                plugin-dispatcher = ""akka.actor.default-dispatcher""
                                table-name = {TABLE_NAME}
                                schema-name = {SCHEMA_NAME}
                                auto-initialize = on
								# NOTE: use either connection-string-name to get a connection string from config, or use connection-string directly
                                # connection-string-name = ""TestDb""
								# connection-string = ""
                            }
                        }
                    }
```
SnapshotStore:

The SnapshotStore uses a table defined by your HOCON. The Table to be used should be set up in a manner similar to what you see in CREATE_SNAPSHOT_TABLE.SQL

Example HOCON Snippet for SnapshotStore. Replace {TABLE_NAME} and {SCHEMA_NAME} with your desired table/schema as desired.
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
                                    auto-initialize = on
                                    # NOTE: use either connection-string-name to get a connection string from config, or use connection-string directly
                                    # connection-string-name = ""TestDb""
								    # connection-string = ""
                                }
                            }
                        }
```