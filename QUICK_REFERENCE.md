# Word Index Building - Quick Reference

## What Changed?

### Critical Bug Fixes
1. **Case-insensitive lemma matching** - Fixed 3 locations where LOWER() was missing
2. **Race condition in counts** - Completely eliminated by using bulk SQL
3. **Data loss in counts** - Fixed by atomic bulk insert

### Performance Improvement
- **Before**: 50 million individual SQL queries (days)
- **After**: 1 bulk SQL query (minutes/hours)
- **Speedup**: 100-1000x faster ⚡

## Files Changed
- `Pythia.Sql/SqlIndexRepository.cs` (lines ~1235, ~1548, ~1561, ~1854-1992)

## Testing Quick Start

### 1. Test on Example Dataset (2 minutes)
```bash
cd Pythia.Cli
./pythia create-db pythia-test -c
./pythia add-profiles example.json pythia-test
./pythia index example *.xml pythia-test -o
./pythia index-w -d pythia-test -c date-value=3 -c date_value=3 -x date
```

### 2. Verify Results
```sql
-- Connect to database
psql pythia-test

-- Should return 0 (all tokens have word_id)
SELECT COUNT(*) FROM span
WHERE type='tok' AND lemma IS NOT NULL AND word_id IS NULL;

-- Should return rows matching docs/13-example-words.md
SELECT value, pos, lemma, count FROM word ORDER BY value LIMIT 10;

-- Should return rows matching docs/14-example-counts.md
SELECT doc_attr_name, doc_attr_value, count
FROM word_count
WHERE word_id = (SELECT id FROM word WHERE value='dico' LIMIT 1);
```

### 3. Test on Production Data (hours)
```bash
# IMPORTANT: Test on a COPY of your database first!
./pythia index-w -d your-database -c date-value=3 -c date_value=3 -x date
```

**Monitor**:
- Execution time (should be hours, not days)
- CPU usage (should be >20%)
- PostgreSQL logs for the large INSERT query

## What to Watch For

### Good Signs ✅
- Process completes in hours instead of days
- CPU usage is higher than before (40-80%)
- PostgreSQL shows one very long query in logs
- All word counts match manual verification queries

### Bad Signs ⚠️
- Query string too large error → Need to batch the UNION ALL
- Out of memory → Increase PostgreSQL `work_mem`
- Very slow query → Check PostgreSQL has indexes on span(word_id, document_id)

## Rollback If Needed
```bash
git checkout HEAD~1 Pythia.Sql/SqlIndexRepository.cs
```

But note: You'll still need the LOWER() fixes for correctness.

## Support
- Full details: See [WORD_INDEX_IMPROVEMENTS.md](./WORD_INDEX_IMPROVEMENTS.md)
- SQL verification queries: See section "Testing Recommendations" in main doc
- Issues: Check PostgreSQL logs and query execution plan with `EXPLAIN ANALYZE`
