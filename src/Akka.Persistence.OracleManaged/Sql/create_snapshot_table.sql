DECLARE
    table_count integer;
BEGIN    
    SELECT COUNT (OBJECT_ID) INTO table_count FROM USER_OBJECTS WHERE EXISTS (
        SELECT OBJECT_NAME FROM USER_OBJECTS WHERE (OBJECT_NAME = '{{TABLE_NAME}}' AND OBJECT_TYPE = 'TABLE'));
    IF table_count = 0 THEN 
        DBMS_OUTPUT.PUT_LINE ('Creating the {{TABLE_NAME}} table');
        EXECUTE IMMEDIATE(
            'CREATE TABLE {{TABLE_NAME}} (
              PersistenceID NVARCHAR2(200) NOT NULL,
              SequenceNr NUMBER(19) NOT NULL,
              Timestamp TimeStamp not null,
              Manifest NVARCHAR2(500) NOT NULL,
              Snapshot LONG RAW NOT NULL,
              CONSTRAINT PK_{{TABLE_NAME}} PRIMARY KEY (PersistenceID, SequenceNr)
            )'
        );
        EXECUTE IMMEDIATE ('CREATE INDEX IX_{{TABLE_NAME}}_SN ON {{TABLE_NAME}}(SequenceNr)');
        EXECUTE IMMEDIATE ('CREATE INDEX IX_{{TABLE_NAME}}_TS ON {{TABLE_NAME}}(Timestamp)');
    ELSE
        DBMS_OUTPUT.PUT_LINE ('The {{TABLE_NAME}} table already exist in the database.');           
    END IF;
END;