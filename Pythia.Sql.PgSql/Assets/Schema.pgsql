-- this schema gets added to the corpus schema

-- install fuzzystrmatch module
-- https://www.postgresql.org/docs/13/fuzzystrmatch.html
CREATE EXTENSION pg_trgm;
-- CREATE EXTENSION fuzzystrmatch;

-- span
CREATE TABLE "span" (
	id serial NOT NULL,
	document_id int4 NOT NULL,
	type varchar(50) NOT NULL,
	p1 int4 NOT NULL,
	p2 int4 NOT NULL,
	"index" int4 NOT NULL,
	length int2 NOT NULL,
	"language" varchar(50) NULL,
	pos varchar(50) NULL,
	value varchar(500) NOT NULL,
	text varchar(1000) NOT NULL,
	CONSTRAINT span_pk PRIMARY KEY (id)
);
CREATE INDEX span_type_idx ON "span" (type);
CREATE INDEX span_p1_idx ON "span" (p1);
CREATE INDEX span_p2_idx ON "span" (p2);
CREATE INDEX span_language_idx ON "span" ("language");
CREATE INDEX span_pos_idx ON "span" (pos);
CREATE INDEX span_value_idx ON "span" (value);
-- span foreign keys
ALTER TABLE "span" ADD CONSTRAINT span_fk FOREIGN KEY (document_id) REFERENCES document(id) ON DELETE CASCADE ON UPDATE CASCADE;

-- span_attribute
CREATE TABLE span_attribute (
	id serial NOT NULL,
	span_id int4 NOT NULL,
	"name" varchar(100) NOT NULL,
	value varchar(500) NOT NULL,
	"type" int4 NOT NULL,
	CONSTRAINT span_attribute_pk PRIMARY KEY (id)
);
CREATE INDEX span_attribute_name_idx ON span_attribute USING btree (name);
CREATE INDEX span_attribute_value_idx ON span_attribute USING btree (value);
CREATE INDEX span_attribute_span_id_idx ON span_attribute (span_id);
-- span_attribute foreign keys
ALTER TABLE span_attribute ADD CONSTRAINT span_attribute_fk FOREIGN KEY (span_id) REFERENCES span(id) ON DELETE CASCADE ON UPDATE CASCADE;
