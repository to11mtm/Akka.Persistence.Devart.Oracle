DECLARE
    table_count integer;
BEGIN    
    SELECT COUNT (OBJECT_ID) INTO table_count FROM USER_OBJECTS WHERE EXISTS (
        SELECT OBJECT_NAME FROM USER_OBJECTS WHERE (upper(OBJECT_NAME) = upper('{{TABLE_NAME}}') AND OBJECT_TYPE = 'TABLE'));
    IF table_count = 0 THEN 
        DBMS_OUTPUT.PUT_LINE ('Creating the {{TABLE_NAME}} table');
        EXECUTE IMMEDIATE(
            'CREATE TABLE {{TABLE_NAME}} (
              Persistence_ID NVARCHAR2(200) NOT NULL,
              Sequence_Nr NUMBER(19) NOT NULL,
              Is_Deleted CHAR(1) default ''N'' CHECK (Is_Deleted IN (''Y'',''N'')) NOT NULL,
              Time_stamp TimeStamp(9) not null,
              Manifest NVARCHAR2(500) NOT NULL,
              Payload LONG RAW NOT NULL,
              CONSTRAINT PK_{{TABLE_NAME}} PRIMARY KEY (Persistence_ID, Sequence_Nr)
            )'
        );
        EXECUTE IMMEDIATE ('CREATE INDEX IX_{{TABLE_NAME}}_SN ON {{TABLE_NAME}}(Sequence_Nr)');
        EXECUTE IMMEDIATE ('CREATE INDEX IX_{{TABLE_NAME}}_TS ON {{TABLE_NAME}}(Time_stamp)');
    ELSE
        DBMS_OUTPUT.PUT_LINE ('The {{TABLE_NAME}} table already exist in the database.');           
    END IF;
END;