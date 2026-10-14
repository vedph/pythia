-- ============================================================================
-- Pythia: indexes migration for existing databases (PostgreSQL 12+)
-- ============================================================================
-- This script brings the indexes of an existing Pythia database in line with
-- the current schema (Corpus.Sql.PgSql and Pythia.Sql.PgSql Schema.pgsql).
-- It is generated from the schema files, so do not edit it by hand.
--
-- - it can be run any number of times: missing indexes are created, and
--   indexes superseded by new ones are dropped after creating their
--   replacements, so that queries can always use an index.
-- - indexes are created CONCURRENTLY, so that the database stays usable while
--   the script runs. This requires running each statement in its own
--   transaction, e.g.: psql -d pythia -f add-performance-indexes.sql
--   (do not use --single-transaction).
-- - if a CONCURRENTLY build fails (e.g. it is interrupted), it leaves an
--   INVALID index: drop it and run the script again.
-- - after running it, run ANALYZE so that the planner gets statistics for
--   the new expression indexes.
-- ============================================================================

CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- ----------------------------------------------------------------------------
-- 1. create the missing indexes
-- ----------------------------------------------------------------------------

CREATE INDEX CONCURRENTLY IF NOT EXISTS document_source_idx ON public.document USING btree (source);

CREATE INDEX CONCURRENTLY IF NOT EXISTS document_user_id_idx ON public.document USING btree (user_id);

CREATE INDEX CONCURRENTLY IF NOT EXISTS document_last_modified_idx ON public."document" (last_modified);

CREATE INDEX CONCURRENTLY IF NOT EXISTS document_author_sort_idx ON public.document USING btree (author, title, date_value, id);

CREATE INDEX CONCURRENTLY IF NOT EXISTS document_author_sort_desc_idx ON public.document USING btree (author, title DESC, date_value DESC, id DESC);

CREATE INDEX CONCURRENTLY IF NOT EXISTS document_title_sort_idx ON public.document USING btree (title, author, date_value, id);

CREATE INDEX CONCURRENTLY IF NOT EXISTS document_title_sort_desc_idx ON public.document USING btree (title, author DESC, date_value DESC, id DESC);

CREATE INDEX CONCURRENTLY IF NOT EXISTS document_date_value_sort_idx ON public.document USING btree (date_value, author, title, id);

CREATE INDEX CONCURRENTLY IF NOT EXISTS document_date_value_sort_desc_idx ON public.document USING btree (date_value, author DESC, title DESC, id DESC);

CREATE INDEX CONCURRENTLY IF NOT EXISTS document_sort_key_id_idx ON public.document USING btree (sort_key, id);

CREATE INDEX CONCURRENTLY IF NOT EXISTS document_profile_id_idx ON public.document USING btree (profile_id varchar_pattern_ops);

CREATE INDEX CONCURRENTLY IF NOT EXISTS document_author_lower_idx ON public.document USING btree (LOWER(author));

CREATE INDEX CONCURRENTLY IF NOT EXISTS document_title_lower_idx ON public.document USING btree (LOWER(title));

CREATE INDEX CONCURRENTLY IF NOT EXISTS document_source_lower_idx ON public.document USING btree (LOWER(source));

CREATE INDEX CONCURRENTLY IF NOT EXISTS document_profile_id_lower_idx ON public.document USING btree (LOWER(profile_id));

CREATE INDEX CONCURRENTLY IF NOT EXISTS document_sort_key_lower_idx ON public.document USING btree (LOWER(sort_key));

CREATE INDEX CONCURRENTLY IF NOT EXISTS document_author_lower_trgm_idx ON public.document USING gin (LOWER(author) gin_trgm_ops);

CREATE INDEX CONCURRENTLY IF NOT EXISTS document_title_lower_trgm_idx ON public.document USING gin (LOWER(title) gin_trgm_ops);

CREATE INDEX CONCURRENTLY IF NOT EXISTS document_source_lower_trgm_idx ON public.document USING gin (LOWER(source) gin_trgm_ops);

CREATE INDEX CONCURRENTLY IF NOT EXISTS document_attribute_document_id_name_idx ON public.document_attribute (document_id, name);

CREATE INDEX CONCURRENTLY IF NOT EXISTS document_attribute_name_value_idx ON public.document_attribute USING btree (name, value, document_id);

CREATE INDEX CONCURRENTLY IF NOT EXISTS document_attribute_value_idx ON public.document_attribute USING btree (value);

CREATE INDEX CONCURRENTLY IF NOT EXISTS document_attribute_name_value_lower_idx ON public.document_attribute USING btree (LOWER(name), LOWER(value), document_id);

CREATE INDEX CONCURRENTLY IF NOT EXISTS document_attribute_value_lower_trgm_idx ON public.document_attribute USING gin (LOWER(value) gin_trgm_ops);

CREATE INDEX CONCURRENTLY IF NOT EXISTS document_attribute_name_lower_trgm_idx ON public.document_attribute USING gin (LOWER(name) gin_trgm_ops);

CREATE INDEX CONCURRENTLY IF NOT EXISTS corpus_title_idx ON public.corpus (title);

CREATE INDEX CONCURRENTLY IF NOT EXISTS document_corpus_corpus_id_idx ON public.document_corpus (corpus_id, document_id);

CREATE INDEX CONCURRENTLY IF NOT EXISTS "email_index" ON public.app_user USING btree (normalized_email);

CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS "user_name_index" ON public.app_user USING btree (normalized_user_name);

CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS "role_name_index" ON public.app_role USING btree (normalized_name);

CREATE INDEX CONCURRENTLY IF NOT EXISTS "ix_app_role_claim_role_id" ON public."app_role_claim" USING btree (role_id);

CREATE INDEX CONCURRENTLY IF NOT EXISTS "ix_app_user_claim_user_id" ON public."app_user_claim" USING btree (user_id);

CREATE INDEX CONCURRENTLY IF NOT EXISTS "ix_app_user_login_user_id" ON public."app_user_login" USING btree (user_id);

CREATE INDEX CONCURRENTLY IF NOT EXISTS "ix_app_user_role_role_id" ON public."app_user_role" USING btree (role_id);

CREATE INDEX CONCURRENTLY IF NOT EXISTS lemma_value_id_idx ON lemma USING btree (value, id);

CREATE INDEX CONCURRENTLY IF NOT EXISTS lemma_reversed_value_id_idx ON lemma USING btree (reversed_value, id);

CREATE INDEX CONCURRENTLY IF NOT EXISTS lemma_count_idx ON lemma USING btree ("count", id);

CREATE INDEX CONCURRENTLY IF NOT EXISTS lemma_value_pattern_idx ON lemma USING btree (value varchar_pattern_ops);

CREATE INDEX CONCURRENTLY IF NOT EXISTS lemma_reversed_value_pattern_idx ON lemma USING btree (reversed_value varchar_pattern_ops);

CREATE INDEX CONCURRENTLY IF NOT EXISTS lemma_value_trgm_idx ON lemma USING gin (value gin_trgm_ops);

CREATE INDEX CONCURRENTLY IF NOT EXISTS lemma_value_length_idx ON lemma USING btree (LENGTH(value));

CREATE INDEX CONCURRENTLY IF NOT EXISTS lemma_pos_idx ON lemma USING btree (pos);

CREATE INDEX CONCURRENTLY IF NOT EXISTS lemma_language_idx ON lemma USING btree (language);

CREATE INDEX CONCURRENTLY IF NOT EXISTS lemma_value_language_pos_idx ON lemma USING btree (value, "language", pos);

CREATE INDEX CONCURRENTLY IF NOT EXISTS word_value_id_idx ON word USING btree (value, id);

CREATE INDEX CONCURRENTLY IF NOT EXISTS word_reversed_value_id_idx ON word USING btree (reversed_value, id);

CREATE INDEX CONCURRENTLY IF NOT EXISTS word_count_idx ON word USING btree ("count", id);

CREATE INDEX CONCURRENTLY IF NOT EXISTS word_value_pattern_idx ON word USING btree (value varchar_pattern_ops);

CREATE INDEX CONCURRENTLY IF NOT EXISTS word_reversed_value_pattern_idx ON word USING btree (reversed_value varchar_pattern_ops);

CREATE INDEX CONCURRENTLY IF NOT EXISTS word_value_trgm_idx ON word USING gin (value gin_trgm_ops);

CREATE INDEX CONCURRENTLY IF NOT EXISTS word_value_length_idx ON word USING btree (LENGTH(value));

CREATE INDEX CONCURRENTLY IF NOT EXISTS word_lemma_id_idx ON word USING btree (lemma_id);

CREATE INDEX CONCURRENTLY IF NOT EXISTS word_pos_idx ON word USING btree (pos);

CREATE INDEX CONCURRENTLY IF NOT EXISTS word_lemma_idx ON word USING btree (lemma);

CREATE INDEX CONCURRENTLY IF NOT EXISTS word_value_language_pos_idx ON word USING btree (value, "language", pos);

CREATE INDEX CONCURRENTLY IF NOT EXISTS lemma_count_lemma_id_da_name_da_value_cnt_idx ON lemma_count USING btree (lemma_id, doc_attr_name, doc_attr_value) INCLUDE ("count");

CREATE INDEX CONCURRENTLY IF NOT EXISTS word_count_word_id_da_name_da_value_cnt_idx ON word_count USING btree (word_id, doc_attr_name, doc_attr_value) INCLUDE ("count");

CREATE INDEX CONCURRENTLY IF NOT EXISTS word_count_lemma_id_idx ON word_count USING btree (lemma_id, doc_attr_name, doc_attr_value) INCLUDE ("count") WHERE lemma_id IS NOT NULL;

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_type_idx ON "span" (type);

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_p1_idx ON "span" (p1);

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_p2_idx ON "span" (p2);

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_language_idx ON "span" ("language");

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_pos_idx ON "span" (pos);

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_lemma_idx ON "span" (lemma);

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_value_idx ON "span" (value);

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_attribute_name_value_span_idx ON span_attribute USING btree (name, value, span_id);

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_attribute_value_idx ON span_attribute USING btree (value);

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_document_p1_p2_idx ON span
USING btree (document_id, p1, p2);

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_type_document_p1_p2_idx ON span
USING btree (type, document_id, p1, p2);

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_type_document_index_idx ON span
USING btree (type, document_id, "index");

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_value_lower_idx ON span
USING btree (LOWER(value));

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_type_value_lower_pattern_idx ON span
USING btree (type, LOWER(value) text_pattern_ops);

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_type_lemma_lower_pattern_idx ON span
USING btree (type, LOWER(lemma) text_pattern_ops)
WHERE lemma IS NOT NULL;

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_type_pos_lower_idx ON span
USING btree (type, LOWER(pos))
WHERE pos IS NOT NULL;

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_type_language_lower_idx ON span
USING btree (type, LOWER(language))
WHERE language IS NOT NULL;

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_lemma_id_idx ON span
USING btree (lemma_id)
WHERE lemma_id IS NOT NULL;

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_word_id_document_id_idx ON span
USING btree (word_id, document_id)
WHERE word_id IS NOT NULL;

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_type_length_idx ON span
USING btree (type, length);

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_value_lower_trgm_idx ON span
USING GIN (LOWER(value) gin_trgm_ops);

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_value_trgm_idx ON span
USING GIN (value gin_trgm_ops);

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_lemma_lower_trgm_idx ON span
USING GIN (LOWER(lemma) gin_trgm_ops)
WHERE lemma IS NOT NULL;

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_lemma_trgm_idx ON span
USING GIN (lemma gin_trgm_ops)
WHERE lemma IS NOT NULL;

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_text_lower_trgm_idx ON span
USING GIN (LOWER(text) gin_trgm_ops);

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_text_trgm_idx ON span
USING GIN (text gin_trgm_ops);

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_attribute_name_value_lower_idx ON span_attribute
USING btree (LOWER(name), LOWER(value) text_pattern_ops, span_id);

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_attribute_span_name_lower_idx ON span_attribute
USING btree (span_id, LOWER(name));

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_attribute_value_lower_trgm_idx ON span_attribute
USING GIN (LOWER(value) gin_trgm_ops);

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_attribute_name_lower_trgm_idx ON span_attribute
USING GIN (LOWER(name) gin_trgm_ops);

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_word_assignment_idx ON span
USING btree (type, COALESCE(language, ''), LOWER(value), COALESCE(pos, ''))
WHERE type = 'tok';

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_lemma_lower_idx ON span
USING btree (LOWER(lemma))
WHERE type = 'tok' AND lemma IS NOT NULL;

CREATE INDEX CONCURRENTLY IF NOT EXISTS word_join_lookup_idx ON word
USING btree (COALESCE(language, ''), value, COALESCE(pos, ''), lemma);

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_lemma_assignment_idx ON span
USING btree (LOWER(lemma), COALESCE(pos, ''), COALESCE(language, ''))
WHERE lemma IS NOT NULL AND word_id IS NOT NULL;

CREATE INDEX CONCURRENTLY IF NOT EXISTS word_lemma_assignment_idx ON word
USING btree (LOWER(lemma), COALESCE(pos, ''), COALESCE(language, ''))
WHERE lemma IS NOT NULL;

CREATE INDEX CONCURRENTLY IF NOT EXISTS lemma_join_lookup_idx ON lemma
USING btree (value, COALESCE(pos, ''), COALESCE(language, ''));

CREATE INDEX CONCURRENTLY IF NOT EXISTS span_attribute_span_name_lookup_idx ON span_attribute
USING btree (span_id, name);

-- ----------------------------------------------------------------------------
-- 2. drop the superseded indexes
-- ----------------------------------------------------------------------------

DROP INDEX CONCURRENTLY IF EXISTS document_author_idx;
DROP INDEX CONCURRENTLY IF EXISTS document_title_idx;
DROP INDEX CONCURRENTLY IF EXISTS document_date_value_idx;
DROP INDEX CONCURRENTLY IF EXISTS document_sort_key_idx;
DROP INDEX CONCURRENTLY IF EXISTS document_attribute_document_id_idx;
DROP INDEX CONCURRENTLY IF EXISTS document_attribute_name_idx;
DROP INDEX CONCURRENTLY IF EXISTS lemma_value_idx;
DROP INDEX CONCURRENTLY IF EXISTS lemma_reversed_value_idx;
DROP INDEX CONCURRENTLY IF EXISTS word_value_idx;
DROP INDEX CONCURRENTLY IF EXISTS word_reversed_value_idx;
DROP INDEX CONCURRENTLY IF EXISTS lemma_count_lemma_id_da_name_da_value_idx;
DROP INDEX CONCURRENTLY IF EXISTS word_count_word_id_da_name_da_value_idx;
DROP INDEX CONCURRENTLY IF EXISTS span_attribute_name_idx;
DROP INDEX CONCURRENTLY IF EXISTS span_attribute_span_id_idx;
DROP INDEX CONCURRENTLY IF EXISTS span_type_value_lower_idx;
DROP INDEX CONCURRENTLY IF EXISTS span_type_lemma_lower_idx;

ANALYZE;
