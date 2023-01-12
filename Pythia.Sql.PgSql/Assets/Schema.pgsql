-- this schema gets added to the corpus schema

-- install fuzzystrmatch module
-- https://www.postgresql.org/docs/13/fuzzystrmatch.html
CREATE EXTENSION pg_trgm;
-- CREATE EXTENSION fuzzystrmatch;

-- token
CREATE TABLE "token" (
	id serial NOT NULL,
	"language" varchar(10) NULL,
	value varchar(300) NOT NULL,
	CONSTRAINT token_pk PRIMARY KEY (id)
);
CREATE INDEX token_value_idx ON "token" (value);
CREATE INDEX token_language_idx ON "token" ("language");

-- occurrence
CREATE TABLE occurrence (
	id serial NOT NULL,
	token_id int4 NOT NULL,
	document_id int4 NOT NULL,
	"position" int4 NOT NULL,
	"index" int4 NOT NULL,
	length int2 NOT NULL,
	CONSTRAINT occurrence_pk PRIMARY KEY (id)
);
ALTER TABLE occurrence ADD CONSTRAINT occurrence_fk FOREIGN KEY (token_id) REFERENCES "token"(id) ON DELETE CASCADE ON UPDATE CASCADE;
ALTER TABLE occurrence ADD CONSTRAINT occurrence_fk_1 FOREIGN KEY (document_id) REFERENCES "document"(id) ON DELETE CASCADE ON UPDATE CASCADE;

-- occurrence_attribute definition
CREATE TABLE occurrence_attribute (
	id serial NOT NULL,
	occurrence_id int4 NOT NULL,
	"name" varchar(100) NOT NULL,
	value varchar(500) NOT NULL,
	"type" int4 NOT NULL,
	CONSTRAINT occurrence_attribute_pk PRIMARY KEY (id)
);
CREATE INDEX occurrence_attribute_name_idx ON occurrence_attribute USING btree (name);
CREATE INDEX occurrence_attribute_value_idx ON occurrence_attribute USING btree (value);
-- occurrence_attribute foreign keys
ALTER TABLE occurrence_attribute ADD CONSTRAINT occurrence_attribute_fk FOREIGN KEY (occurrence_id) REFERENCES occurrence(id) ON DELETE CASCADE ON UPDATE CASCADE;

-- structure
CREATE TABLE "structure" (
	id serial NOT NULL,
	document_id int4 NOT NULL,
	start_position int4 NOT NULL,
	end_position int4 NOT NULL,
	"name" varchar(100) NOT NULL,
	CONSTRAINT structure_pk PRIMARY KEY (id)
);
ALTER TABLE "structure" ADD CONSTRAINT structure_fk FOREIGN KEY (document_id) REFERENCES "document"(id) ON DELETE CASCADE ON UPDATE CASCADE;

-- structure_attribute
CREATE TABLE structure_attribute (
	id serial NOT NULL,
	structure_id int4 NOT NULL,
	"name" varchar(100) NOT NULL,
	value varchar(500) NOT NULL,
	"type" int4 NOT NULL,
	CONSTRAINT structure_attribute_pk PRIMARY KEY (id)
);
CREATE INDEX structure_attribute_name_idx ON structure_attribute USING btree (name);
CREATE INDEX structure_attribute_value_idx ON structure_attribute USING btree (value);
ALTER TABLE structure_attribute ADD CONSTRAINT structure_attribute_fk FOREIGN KEY (structure_id) REFERENCES "structure"(id) ON DELETE CASCADE ON UPDATE CASCADE;

-- document_structure
CREATE TABLE document_structure (
	document_id int4 NOT NULL,
	structure_id int4 NOT NULL,
	"position" int4 NOT NULL,
	CONSTRAINT document_structure_pk PRIMARY KEY (document_id, structure_id, "position")
);
-- document_structure foreign keys
ALTER TABLE document_structure ADD CONSTRAINT document_structure_fk_d FOREIGN KEY (document_id) REFERENCES "document"(id) ON DELETE CASCADE ON UPDATE CASCADE;
ALTER TABLE document_structure ADD CONSTRAINT document_structure_fk_s FOREIGN KEY (structure_id) REFERENCES "structure"(id) ON DELETE CASCADE ON UPDATE CASCADE;

-- token_occurrence_count
CREATE TABLE token_occurrence_count (
	id int4 NOT NULL,
	value varchar(300) NULL,
	count int8 NULL,
	CONSTRAINT token_occurrence_count_pk PRIMARY KEY (id)
);
CREATE INDEX token_occurrence_count_value_idx ON token_occurrence_count USING btree (value);
