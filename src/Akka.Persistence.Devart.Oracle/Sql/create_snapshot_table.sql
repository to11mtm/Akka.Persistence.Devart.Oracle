DECLARE
    table_count integer;
BEGIN    
    SELECT COUNT (OBJECT_ID) INTO table_count FROM USER_OBJECTS WHERE EXISTS (
        SELECT OBJECT_NAME FROM USER_OBJECTS WHERE (upper(OBJECT_NAME) = upper('{{TABLE_NAME}}') AND OBJECT_TYPE = 'TABLE'));
    IF table_count = 0 THEN 
        DBMS_OUTPUT.PUT_LINE ('Creating the {{TABLE_NAME}} table');
        EXECUTE IMMEDIATE(
            'CREATE TABLE {{TABLE_NAME}} (
              persistence_id NVARCHAR2(255) NOT NULL, 
                     sequence_nr number(30) NOT NULL, 
                     time_stamp timestamp(9) NOT NULL, 
                     manifest NVARCHAR2(500) NOT NULL, 
                     snapshot BLOB NOT NULL, 
              CONSTRAINT PK_{{TABLE_NAME}} PRIMARY KEY (persistence_id, sequence_nr)
            )'
        );
        EXECUTE IMMEDIATE ('CREATE INDEX IX_{{TABLE_NAME}}_SN ON {{TABLE_NAME}}(sequence_nr)');
        EXECUTE IMMEDIATE ('CREATE INDEX IX_{{TABLE_NAME}}_TS ON {{TABLE_NAME}}(time_stamp)');
    ELSE
        DBMS_OUTPUT.PUT_LINE ('The {{TABLE_NAME}} table already exist in the database.');           
    END IF;
END;