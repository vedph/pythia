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
	lemma varchar(500) NULL,
	value varchar(500) NOT NULL,
	text varchar(1000) NOT NULL,
	CONSTRAINT span_pk PRIMARY KEY (id)
);
CREATE INDEX span_type_idx ON "span" (type);
CREATE INDEX span_p1_idx ON "span" (p1);
CREATE INDEX span_p2_idx ON "span" (p2);
CREATE INDEX span_language_idx ON "span" ("language");
CREATE INDEX span_pos_idx ON "span" (pos);
CREATE INDEX span_lemma_idx ON "span" (lemma);
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

-- lemma
CREATE TABLE lemma (
	id serial NOT NULL,
	value varchar(500) NOT NULL,
	reversed_value varchar(500) NOT NULL,
	"language" varchar(50) NULL,
	"count" int4 NOT NULL,
	CONSTRAINT lemma_pk PRIMARY KEY (id)
);
CREATE INDEX lemma_value_idx ON lemma USING btree (value);
CREATE INDEX lemma_value_language_idx ON lemma USING btree (value, "language");
CREATE INDEX lemma_reversed_value_idx ON lemma USING btree (reversed_value);

-- word
CREATE TABLE word (
	id serial NOT NULL,
	lemma_id int4 NULL,
	value varchar(500) NOT NULL,
	reversed_value varchar(500) NOT NULL,
	"language" varchar(50) NULL,
	pos varchar(50) NULL,
	lemma varchar(500) NULL,
	"count" int4 NOT NULL,
	CONSTRAINT word_pk PRIMARY KEY (id)
);
CREATE INDEX word_value_idx ON word USING btree (value);
CREATE INDEX word_pos_idx ON word USING btree (pos);
CREATE INDEX word_lemma_idx ON word USING btree (lemma);
CREATE INDEX word_value_language_pos_idx ON word USING btree (value, "language", pos);
CREATE INDEX word_reversed_value_idx ON word USING btree (reversed_value);
-- word foreign keys
ALTER TABLE word ADD CONSTRAINT word_fk FOREIGN KEY (lemma_id) REFERENCES lemma(id) ON DELETE SET NULL ON UPDATE CASCADE;

-- lemma_document
CREATE TABLE lemma_document (
	id serial NOT NULL,
	lemma_id int4 NOT NULL,
	document_id int4 NOT NULL,
	document_attr_name varchar(100) NOT NULL,
	document_attr_value varchar(500) NOT NULL,
	"count" int4 NOT NULL,
	CONSTRAINT lemma_document_pk PRIMARY KEY (id)
);
CREATE INDEX lemma_document_document_id_document_attr_name_document_attr_value_idx ON lemma_document USING btree (document_id, document_attr_name, document_attr_value);
-- foreign keys
ALTER TABLE lemma_document ADD CONSTRAINT lemma_document_fk_lemma FOREIGN KEY (lemma_id) REFERENCES lemma(id) ON DELETE CASCADE ON UPDATE CASCADE;
ALTER TABLE lemma_document ADD CONSTRAINT lemma_document_fk_document FOREIGN KEY (document_id) REFERENCES document(id) ON DELETE CASCADE ON UPDATE CASCADE;

-- word_document
CREATE TABLE word_document (
	id serial NOT NULL,
	word_id int4 NOT NULL,
	document_id int4 NOT NULL,
	document_attr_name varchar(100) NOT NULL,
	document_attr_value varchar(500) NOT NULL,
	"count" int4 NOT NULL,
	CONSTRAINT word_document_pk PRIMARY KEY (id)
);
CREATE INDEX word_document_document_id_document_attr_name_document_attr_value_idx ON word_document USING btree (document_id, document_attr_name, document_attr_value);
-- foreign keys
ALTER TABLE word_document ADD CONSTRAINT word_document_fk_word FOREIGN KEY (word_id) REFERENCES word(id) ON DELETE CASCADE ON UPDATE CASCADE;
ALTER TABLE word_document ADD CONSTRAINT word_document_fk_document FOREIGN KEY (document_id) REFERENCES document(id) ON DELETE CASCADE ON UPDATE CASCADE;
