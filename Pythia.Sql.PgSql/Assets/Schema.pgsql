-- this schema gets added to the corpus schema

-- trigram indexes (pg_trgm) are used for LIKE '%...%', LIKE '%...',
-- wildcards and regular expression searches
CREATE EXTENSION IF NOT EXISTS pg_trgm;
-- CREATE EXTENSION fuzzystrmatch;

-- ============================================================================
-- Index design notes
-- ============================================================================
-- - the database collation is usually not C, so default btree indexes cannot
--   serve LIKE 'prefix%': prefix searches use *_pattern_ops indexes, which
--   also serve equality. Sorting instead requires default btree indexes.
-- - the search query builder compares values case-insensitively, i.e.
--   LOWER(column) = LOWER('...'), so search indexes are built on LOWER().
-- - pg_trgm GIN indexes serve LIKE with leading wildcards, and regular
--   expressions (~). They are not used by similarity() calls.
-- - btree index entries cannot exceed about 2700 bytes: no btree index
--   includes span.text (up to 1000 characters, i.e. up to 4000 bytes).
-- - paging uses ORDER BY ... LIMIT ... OFFSET: indexes matching the sort
--   order (with id as tiebreaker) let PostgreSQL stop early.
-- ============================================================================

-- lemma
CREATE TABLE lemma (
	id serial NOT NULL,
	value varchar(500) NOT NULL,
	reversed_value varchar(500) NOT NULL,
	"language" varchar(50) NULL,
	pos varchar(50) NULL,
	"count" int4 NOT NULL,
	CONSTRAINT lemma_pk PRIMARY KEY (id)
);
-- equality and sorted paging by value, reversed value, or count
CREATE INDEX lemma_value_id_idx ON lemma USING btree (value, id);
CREATE INDEX lemma_reversed_value_id_idx ON lemma USING btree (reversed_value, id);
CREATE INDEX lemma_count_idx ON lemma USING btree ("count", id);
-- prefix searches (value LIKE 'x%'; reversed_value LIKE 'x%' for '%x')
CREATE INDEX lemma_value_pattern_idx ON lemma USING btree (value varchar_pattern_ops);
CREATE INDEX lemma_reversed_value_pattern_idx ON lemma USING btree (reversed_value varchar_pattern_ops);
-- other wildcard searches
CREATE INDEX lemma_value_trgm_idx ON lemma USING gin (value gin_trgm_ops);
-- value length filters
CREATE INDEX lemma_value_length_idx ON lemma USING btree (LENGTH(value));
CREATE INDEX lemma_pos_idx ON lemma USING btree (pos);
CREATE INDEX lemma_language_idx ON lemma USING btree (language);
CREATE INDEX lemma_value_language_pos_idx ON lemma USING btree (value, "language", pos);

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
-- equality and sorted paging by value, reversed value, or count
CREATE INDEX word_value_id_idx ON word USING btree (value, id);
CREATE INDEX word_reversed_value_id_idx ON word USING btree (reversed_value, id);
CREATE INDEX word_count_idx ON word USING btree ("count", id);
-- prefix searches (value LIKE 'x%'; reversed_value LIKE 'x%' for '%x')
CREATE INDEX word_value_pattern_idx ON word USING btree (value varchar_pattern_ops);
CREATE INDEX word_reversed_value_pattern_idx ON word USING btree (reversed_value varchar_pattern_ops);
-- other wildcard searches
CREATE INDEX word_value_trgm_idx ON word USING gin (value gin_trgm_ops);
-- value length filters
CREATE INDEX word_value_length_idx ON word USING btree (LENGTH(value));
-- words of a lemma, and FK
CREATE INDEX word_lemma_id_idx ON word USING btree (lemma_id);
CREATE INDEX word_pos_idx ON word USING btree (pos);
CREATE INDEX word_lemma_idx ON word USING btree (lemma);
CREATE INDEX word_value_language_pos_idx ON word USING btree (value, "language", pos);
-- word foreign keys
ALTER TABLE word ADD CONSTRAINT word_fk FOREIGN KEY (lemma_id) REFERENCES lemma(id) ON DELETE SET NULL ON UPDATE CASCADE;

-- lemma_count
CREATE TABLE lemma_count (
	id serial NOT NULL,
	lemma_id int4 NOT NULL,
	doc_attr_name varchar(100) NOT NULL,
	doc_attr_value varchar(500) NOT NULL,
	"count" int4 NOT NULL,
	CONSTRAINT lemma_count_pk PRIMARY KEY (id)
);
-- counts of a lemma by attribute name: count is included to allow
-- index-only scans
CREATE INDEX lemma_count_lemma_id_da_name_da_value_cnt_idx ON lemma_count USING btree (lemma_id, doc_attr_name, doc_attr_value) INCLUDE ("count");
-- foreign keys
ALTER TABLE lemma_count ADD CONSTRAINT lemma_count_fk_lemma FOREIGN KEY (lemma_id) REFERENCES lemma(id) ON DELETE CASCADE ON UPDATE CASCADE;

-- word_count
CREATE TABLE word_count (
	id serial NOT NULL,
	word_id int4 NOT NULL,
	lemma_id int4 NULL,
	doc_attr_name varchar(100) NOT NULL,
	doc_attr_value varchar(500) NOT NULL,
	"count" int4 NOT NULL,
	CONSTRAINT word_count_pk PRIMARY KEY (id)
);
-- counts of a word by attribute name: count is included to allow
-- index-only scans
CREATE INDEX word_count_word_id_da_name_da_value_cnt_idx ON word_count USING btree (word_id, doc_attr_name, doc_attr_value) INCLUDE ("count");
-- FK, and grouping by lemma when building lemma counts
CREATE INDEX word_count_lemma_id_idx ON word_count USING btree (lemma_id, doc_attr_name, doc_attr_value) INCLUDE ("count") WHERE lemma_id IS NOT NULL;
-- foreign keys
ALTER TABLE word_count ADD CONSTRAINT word_count_fk_word FOREIGN KEY (word_id) REFERENCES word(id) ON DELETE CASCADE ON UPDATE CASCADE;
ALTER TABLE word_count ADD CONSTRAINT word_count_fk_lemma FOREIGN KEY (lemma_id) REFERENCES lemma(id) ON DELETE SET NULL ON UPDATE CASCADE;

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
	lemma_id int4 NULL,
	word_id int4 NULL,
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
ALTER TABLE "span" ADD CONSTRAINT span_document_fk FOREIGN KEY (document_id) REFERENCES document(id) ON DELETE CASCADE ON UPDATE CASCADE;
ALTER TABLE "span" ADD CONSTRAINT span_lemma_fk FOREIGN KEY (lemma_id) REFERENCES lemma(id) ON DELETE SET NULL ON UPDATE CASCADE;
ALTER TABLE "span" ADD CONSTRAINT span_word_fk FOREIGN KEY (word_id) REFERENCES word(id) ON DELETE SET NULL ON UPDATE CASCADE;

-- span_attribute
CREATE TABLE span_attribute (
	id serial NOT NULL,
	span_id int4 NOT NULL,
	"name" varchar(100) NOT NULL,
	value varchar(500) NOT NULL,
	"type" int4 NOT NULL,
	CONSTRAINT span_attribute_pk PRIMARY KEY (id)
);
-- exact name (and value) lookups (spans enumeration, attribute names)
CREATE INDEX span_attribute_name_value_span_idx ON span_attribute USING btree (name, value, span_id);
CREATE INDEX span_attribute_value_idx ON span_attribute USING btree (value);
-- span_attribute foreign keys
ALTER TABLE span_attribute ADD CONSTRAINT span_attribute_fk FOREIGN KEY (span_id) REFERENCES span(id) ON DELETE CASCADE ON UPDATE CASCADE;

-- ============================================================================
-- Performance indexes for search queries
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 1. SPAN LOOKUPS BY DOCUMENT AND POSITION
-- ----------------------------------------------------------------------------

-- spans of a document (FK, including cascaded deletes of documents), spans
-- at a position (GetSpansAt), and joins of spans with document filters
CREATE INDEX IF NOT EXISTS span_document_p1_p2_idx ON span
USING btree (document_id, p1, p2);

-- KWIC context and token ranges:
--   WHERE type='tok' AND document_id=N AND p1 >= A AND p2 <= B
CREATE INDEX IF NOT EXISTS span_type_document_p1_p2_idx ON span
USING btree (type, document_id, p1, p2);

-- positions from character indexes (GetPositionRange):
--   WHERE document_id=N AND type='tok' AND index >= A AND index <= B
CREATE INDEX IF NOT EXISTS span_type_document_index_idx ON span
USING btree (type, document_id, "index");

-- ----------------------------------------------------------------------------
-- 2. CASE-INSENSITIVE EQUALITY AND PREFIX SEARCHES ON SPAN ATTRIBUTES
-- ----------------------------------------------------------------------------
-- Every pair is like: type='<type>' AND LOWER(span.<attr>) = LOWER('...'),
-- or LOWER(span.<attr>) LIKE 'prefix%'. text_pattern_ops serves both.

CREATE INDEX IF NOT EXISTS span_value_lower_idx ON span
USING btree (LOWER(value));

CREATE INDEX IF NOT EXISTS span_type_value_lower_pattern_idx ON span
USING btree (type, LOWER(value) text_pattern_ops);

CREATE INDEX IF NOT EXISTS span_type_lemma_lower_pattern_idx ON span
USING btree (type, LOWER(lemma) text_pattern_ops)
WHERE lemma IS NOT NULL;

CREATE INDEX IF NOT EXISTS span_type_pos_lower_idx ON span
USING btree (type, LOWER(pos))
WHERE pos IS NOT NULL;

CREATE INDEX IF NOT EXISTS span_type_language_lower_idx ON span
USING btree (type, LOWER(language))
WHERE language IS NOT NULL;

-- ----------------------------------------------------------------------------
-- 3. NUMERIC SPAN ATTRIBUTES
-- ----------------------------------------------------------------------------

-- [lemma_id="N"] (also FK)
CREATE INDEX IF NOT EXISTS span_lemma_id_idx ON span
USING btree (lemma_id)
WHERE lemma_id IS NOT NULL;

-- [word_id="N"] (also FK), and word counts by document
CREATE INDEX IF NOT EXISTS span_word_id_document_id_idx ON span
USING btree (word_id, document_id)
WHERE word_id IS NOT NULL;

-- [length>N] and the like
CREATE INDEX IF NOT EXISTS span_type_length_idx ON span
USING btree (type, length);

-- ----------------------------------------------------------------------------
-- 4. TRIGRAM (GIN) INDEXES FOR WILDCARDS AND REGULAR EXPRESSIONS
-- ----------------------------------------------------------------------------
-- LIKE '%x%' (*=), LIKE '%x' ($=) and wildcards (?=) use LOWER(); regular
-- expressions (~=) use the column as is. pg_trgm needs at least 3 characters
-- in the pattern to use the index.

CREATE INDEX IF NOT EXISTS span_value_lower_trgm_idx ON span
USING GIN (LOWER(value) gin_trgm_ops);

CREATE INDEX IF NOT EXISTS span_value_trgm_idx ON span
USING GIN (value gin_trgm_ops);

CREATE INDEX IF NOT EXISTS span_lemma_lower_trgm_idx ON span
USING GIN (LOWER(lemma) gin_trgm_ops)
WHERE lemma IS NOT NULL;

CREATE INDEX IF NOT EXISTS span_lemma_trgm_idx ON span
USING GIN (lemma gin_trgm_ops)
WHERE lemma IS NOT NULL;

-- text is also used for equality, as it is too long for a btree index
CREATE INDEX IF NOT EXISTS span_text_lower_trgm_idx ON span
USING GIN (LOWER(text) gin_trgm_ops);

CREATE INDEX IF NOT EXISTS span_text_trgm_idx ON span
USING GIN (text gin_trgm_ops);

-- ----------------------------------------------------------------------------
-- 5. NON-PRIVILEGED SPAN ATTRIBUTES (span_attribute)
-- ----------------------------------------------------------------------------
-- Search pairs are like:
--   EXISTS (SELECT * FROM span_attribute sa WHERE sa.span_id=span.id
--   AND LOWER(sa.name)=LOWER('...') [AND LOWER(value)=LOWER('...')])

-- attribute-driven lookup: from name (and value) to spans
CREATE INDEX IF NOT EXISTS span_attribute_name_value_lower_idx ON span_attribute
USING btree (LOWER(name), LOWER(value) text_pattern_ops, span_id);

-- span-driven lookup: the attribute of a span (case-insensitive name)
CREATE INDEX IF NOT EXISTS span_attribute_span_name_lower_idx ON span_attribute
USING btree (span_id, LOWER(name));

-- attribute values with leading wildcards or regular expressions
CREATE INDEX IF NOT EXISTS span_attribute_value_lower_trgm_idx ON span_attribute
USING GIN (LOWER(value) gin_trgm_ops);

-- attribute names filters (LOWER(name) LIKE '%...%')
CREATE INDEX IF NOT EXISTS span_attribute_name_lower_trgm_idx ON span_attribute
USING GIN (LOWER(name) gin_trgm_ops);

-- ============================================================================
-- Performance indexes for building the word index (BuildWordIndexAsync)
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 1. WORD ASSIGNMENT INDEXES (AssignWordIdsAsync)
-- ----------------------------------------------------------------------------

-- span-to-word join: type='tok', language, LOWER(value), pos
CREATE INDEX IF NOT EXISTS span_word_assignment_idx ON span
USING btree (type, COALESCE(language, ''), LOWER(value), COALESCE(pos, ''))
WHERE type = 'tok';

-- case-insensitive lemma matching in word assignment
CREATE INDEX IF NOT EXISTS span_lemma_lower_idx ON span
USING btree (LOWER(lemma))
WHERE type = 'tok' AND lemma IS NOT NULL;

-- word side of the span-to-word join
CREATE INDEX IF NOT EXISTS word_join_lookup_idx ON word
USING btree (COALESCE(language, ''), value, COALESCE(pos, ''), lemma);

-- ----------------------------------------------------------------------------
-- 2. LEMMA ASSIGNMENT INDEXES (InsertLemmataAsync)
-- ----------------------------------------------------------------------------

-- span-to-lemma join: LOWER(lemma), pos, language, word_id IS NOT NULL
CREATE INDEX IF NOT EXISTS span_lemma_assignment_idx ON span
USING btree (LOWER(lemma), COALESCE(pos, ''), COALESCE(language, ''))
WHERE lemma IS NOT NULL AND word_id IS NOT NULL;

-- word-to-lemma join: LOWER(lemma), pos, language
CREATE INDEX IF NOT EXISTS word_lemma_assignment_idx ON word
USING btree (LOWER(lemma), COALESCE(pos, ''), COALESCE(language, ''))
WHERE lemma IS NOT NULL;

-- lemma side of the joins
CREATE INDEX IF NOT EXISTS lemma_join_lookup_idx ON lemma
USING btree (value, COALESCE(pos, ''), COALESCE(language, ''));

-- ----------------------------------------------------------------------------
-- 3. SPAN ATTRIBUTES EXCLUSION (NOT EXISTS ... sa.name IN (...)), also used
--    as FK index and for deleting attributes of spans
-- ----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS span_attribute_span_name_lookup_idx ON span_attribute
USING btree (span_id, name);
