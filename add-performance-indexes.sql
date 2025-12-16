-- ============================================================================
-- Performance Indexes for BuildWordIndexAsync
-- ============================================================================
-- This script adds indexes to optimize the long-running UPDATE operations
-- in the word index building process.
--
-- Execute this script on your database BEFORE running BuildWordIndexAsync
-- to prevent timeout errors on large datasets (1M+ spans).
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 1. WORD ASSIGNMENT INDEXES (for AssignWordIdsAsync)
-- ----------------------------------------------------------------------------

-- Composite index for span-to-word join (AssignWordIdsAsync line 1232-1237)
-- Supports: type='tok', language match, LOWER(value) match, pos match
CREATE INDEX IF NOT EXISTS span_word_assignment_idx ON span
USING btree (type, COALESCE(language, ''), LOWER(value), COALESCE(pos, ''))
WHERE type = 'tok';

-- Expression index for case-insensitive lemma matching in word assignment
CREATE INDEX IF NOT EXISTS span_lemma_lower_idx ON span
USING btree (LOWER(lemma))
WHERE type = 'tok' AND lemma IS NOT NULL;

-- Composite index on word table to speed up the join from span side
CREATE INDEX IF NOT EXISTS word_join_lookup_idx ON word
USING btree (COALESCE(language, ''), value, COALESCE(pos, ''), lemma);

-- ----------------------------------------------------------------------------
-- 2. LEMMA ASSIGNMENT INDEXES (for InsertLemmataAsync)
-- ----------------------------------------------------------------------------

-- Composite index for span-to-lemma join (InsertLemmataAsync line 1558-1566)
-- Supports: LOWER(lemma) match, pos match, language match, word_id IS NOT NULL
CREATE INDEX IF NOT EXISTS span_lemma_assignment_idx ON span
USING btree (LOWER(lemma), COALESCE(pos, ''), COALESCE(language, ''))
WHERE lemma IS NOT NULL AND word_id IS NOT NULL;

-- Composite index for word-to-lemma join (InsertLemmataAsync line 1544-1549)
-- Supports: LOWER(lemma) match, pos match, language match
CREATE INDEX IF NOT EXISTS word_lemma_assignment_idx ON word
USING btree (LOWER(lemma), COALESCE(pos, ''), COALESCE(language, ''))
WHERE lemma IS NOT NULL;

-- Composite index on lemma table to speed up joins
CREATE INDEX IF NOT EXISTS lemma_join_lookup_idx ON lemma
USING btree (value, COALESCE(pos, ''), COALESCE(language, ''));

-- ----------------------------------------------------------------------------
-- 3. WORD COUNT INDEXES (for InsertWordCountsAsync)
-- ----------------------------------------------------------------------------

-- These should already exist from schema, but verify:
-- word_count_word_id_da_name_da_value_idx
-- lemma_count_lemma_id_da_name_da_value_idx

-- Additional index to speed up the UNION ALL queries in InsertWordCountsAsync
CREATE INDEX IF NOT EXISTS span_word_id_document_id_idx ON span
USING btree (word_id, document_id)
WHERE word_id IS NOT NULL;

-- ----------------------------------------------------------------------------
-- 4. SPAN_ATTRIBUTE INDEXES (for NOT EXISTS checks with excluded attributes)
-- ----------------------------------------------------------------------------

-- Composite index for span_attribute lookups by span_id and name
-- This helps with the NOT EXISTS clauses in AssignWordIdsAsync
CREATE INDEX IF NOT EXISTS span_attribute_span_name_lookup_idx ON span_attribute
USING btree (span_id, name);

-- ----------------------------------------------------------------------------
-- VERIFICATION QUERIES
-- ----------------------------------------------------------------------------

-- Run these queries to verify indexes were created:

-- Check span indexes:
-- SELECT indexname, indexdef FROM pg_indexes
-- WHERE tablename = 'span' AND indexname LIKE '%assignment%'
-- ORDER BY indexname;

-- Check word indexes:
-- SELECT indexname, indexdef FROM pg_indexes
-- WHERE tablename = 'word' AND indexname LIKE '%assignment%'
-- ORDER BY indexname;

-- Check lemma indexes:
-- SELECT indexname, indexdef FROM pg_indexes
-- WHERE tablename = 'lemma' AND indexname LIKE '%lookup%'
-- ORDER BY indexname;

-- Check all new indexes:
-- SELECT schemaname, tablename, indexname, indexdef
-- FROM pg_indexes
-- WHERE indexname IN (
--   'span_word_assignment_idx',
--   'span_lemma_lower_idx',
--   'word_join_lookup_idx',
--   'span_lemma_assignment_idx',
--   'word_lemma_assignment_idx',
--   'lemma_join_lookup_idx',
--   'span_word_id_document_id_idx',
--   'span_attribute_span_name_lookup_idx'
-- )
-- ORDER BY tablename, indexname;

-- ============================================================================
-- ANALYSIS QUERIES (to verify performance improvements)
-- ============================================================================

-- 1. Test AssignWordIdsAsync query plan (should show Index Scan, not Seq Scan):
-- EXPLAIN (ANALYZE false, VERBOSE, BUFFERS)
-- UPDATE span SET word_id = word.id
-- FROM word
-- WHERE span.type = 'tok'
--   AND COALESCE(span.language, '') = COALESCE(word.language, '')
--   AND LOWER(span.value) = word.value
--   AND COALESCE(span.pos, '') = COALESCE(word.pos, '')
--   AND LOWER(span.lemma) = word.lemma;

-- 2. Test InsertLemmataAsync span update query plan:
-- EXPLAIN (ANALYZE false, VERBOSE, BUFFERS)
-- UPDATE span SET lemma_id=lemma.id
-- FROM lemma
-- WHERE COALESCE(span.pos,'')=COALESCE(lemma.pos, '')
--   AND COALESCE(span.language,'')=COALESCE(lemma.language, '')
--   AND LOWER(span.lemma) = lemma.value
--   AND span.lemma IS NOT NULL
--   AND span.word_id IS NOT NULL;

-- 3. Test InsertLemmataAsync word update query plan:
-- EXPLAIN (ANALYZE false, VERBOSE, BUFFERS)
-- UPDATE word SET lemma_id=lemma.id
-- FROM lemma
-- WHERE COALESCE(word.pos,'')=COALESCE(lemma.pos, '')
--   AND COALESCE(word.language,'')=COALESCE(lemma.language, '')
--   AND LOWER(word.lemma) = lemma.value
--   AND word.lemma IS NOT NULL;

-- ============================================================================
-- MAINTENANCE
-- ============================================================================

-- After creating indexes, update statistics for the query planner:
ANALYZE span;
ANALYZE word;
ANALYZE lemma;
ANALYZE span_attribute;
ANALYZE word_count;
ANALYZE lemma_count;

-- ============================================================================
-- NOTES
-- ============================================================================
-- * Indexes use COALESCE to handle NULL values in language and pos fields
-- * Expression indexes (LOWER) are essential for case-insensitive matching
-- * Partial indexes (WHERE clauses) reduce index size and improve performance
-- * The CommandTimeout is set to 3600 seconds (1 hour) in the C# code
-- * Expected performance: ~48 seconds for AssignWordIdsAsync with 1.5M spans
-- ============================================================================
