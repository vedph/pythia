-----------------------------------------------------------
-- Pythia functions.
-- All the functions are plain SQL expressions, so that the query planner
-- can inline them. The search query builder does not call the location
-- functions: it emits equivalent conditions on positions, which can use
-- indexes. These functions are kept for third-party SQL code.
-- Note: they are not STRICT, as this would prevent inlining.
-----------------------------------------------------------

-----------------------------------------------------------
-- boolean pyt_is_numeric(text)
-- Rets true if the specified text is a number, i.e. an optional sign
-- followed by digits with an optional decimal part, or by a decimal part
-- only, and an optional exponent; leading and trailing whitespace is allowed.
-- Returns null for null.

CREATE OR REPLACE FUNCTION pyt_is_numeric(text) RETURNS BOOLEAN AS $$
  SELECT $1 ~ '^\s*[-+]?([0-9]+(\.[0-9]*)?|\.[0-9]+)([eE][-+]?[0-9]+)?\s*$';
$$
LANGUAGE sql IMMUTABLE PARALLEL SAFE;

-----------------------------------------------------------
-- int pyt_min
-- Returns the minimum between two integers.
-- a: the first integer.
-- b: the second integer.

CREATE OR REPLACE FUNCTION public.pyt_min(a INT, b INT) RETURNS INT AS $$
  SELECT LEAST(a, b);
$$
LANGUAGE sql IMMUTABLE PARALLEL SAFE;

-----------------------------------------------------------
-- int pyt_max
-- Returns the maximum between two integers.
-- a: the first integer.
-- b: the second integer.

CREATE OR REPLACE FUNCTION public.pyt_max(a INT, b INT) RETURNS INT AS $$
  SELECT GREATEST(a, b);
$$
LANGUAGE sql IMMUTABLE PARALLEL SAFE;

-----------------------------------------------------------
-- boolean pyt_is_overlap
-- Determines whether spans A and B overlap, i.e. share at least a position.
-- a1: the A span start
-- a2: the A span end
-- b1: the B span start
-- b2: the B span end

CREATE OR REPLACE FUNCTION pyt_is_overlap(a1 INT, a2 INT, b1 INT, b2 INT)
RETURNS BOOLEAN AS $$
  SELECT b1 <= a2 AND b2 >= a1;
$$
LANGUAGE sql IMMUTABLE PARALLEL SAFE;

-----------------------------------------------------------
-- int pyt_get_overlap_count
-- Gets the count of positions of span A overlapping with positions of span B.
-- a1: the A span start
-- a2: the A span end
-- b1: the B span start
-- b2: the B span end

CREATE OR REPLACE FUNCTION pyt_get_overlap_count(a1 INT, a2 INT, b1 INT, b2 INT)
RETURNS INT AS $$
  SELECT GREATEST(0, LEAST(a2, b2) - GREATEST(a1, b1) + 1);
$$
LANGUAGE sql IMMUTABLE PARALLEL SAFE;

-----------------------------------------------------------
-- boolean pyt_is_overlap_within
-- Determines whether spans A and B overlap within the specified extent.
-- a1: the A span start
-- a2: the A span end
-- b1: the B span start
-- b2: the B span end
-- n: the minimum required overlap count
-- m: the maximum allowed overlap count

CREATE OR REPLACE FUNCTION pyt_is_overlap_within(a1 INT, a2 INT, b1 INT, b2 INT, n INT, m INT)
RETURNS BOOLEAN AS $$
  SELECT b1 <= a2 AND b2 >= a1
    AND LEAST(a2, b2) - GREATEST(a1, b1) + 1 BETWEEN n AND m;
$$
LANGUAGE sql IMMUTABLE PARALLEL SAFE;

-----------------------------------------------------------
-- boolean pyt_is_inside_within
-- Determines whether span A is fully inside span B, within the specified distances.
-- a1: the A span start
-- a2: the A span end
-- b1: the B span start
-- b2: the B span end
-- ns: The min required distance of A from B start
-- ms: The max required distance of A from B start
-- ne: The min required distance of A from B end
-- me: The max required distance of A from B end

CREATE OR REPLACE FUNCTION pyt_is_inside_within(a1 INT, a2 INT, b1 INT, b2 INT,
  ns INT, ms INT, ne INT, me INT) RETURNS BOOLEAN AS $$
  SELECT a1 >= b1 AND a2 <= b2
    AND a1 - b1 BETWEEN ns AND ms
    AND b2 - a2 BETWEEN ne AND me;
$$
LANGUAGE sql IMMUTABLE PARALLEL SAFE;

-----------------------------------------------------------
-- pyt_is_before_within
-- Determines whether span A is before span B within the specified distance,
-- i.e. the count of positions between the end of A and the start of B.
-- a1: the A span start
-- a2: the A span end
-- b1: the B span start
-- b2: the B span end
-- n: the min required distance (0-N)
-- m: the max required distance (0-N)

CREATE OR REPLACE FUNCTION pyt_is_before_within(a1 INT, a2 INT, b1 INT, b2 INT,
  n INT, m INT)
RETURNS BOOLEAN AS $$
  SELECT a2 < b1 AND b1 - a2 - 1 BETWEEN n AND m;
$$
LANGUAGE sql IMMUTABLE PARALLEL SAFE;

-----------------------------------------------------------
-- pyt_is_after_within
-- Determines whether span A is after span B within the specified distance,
-- i.e. the count of positions between the end of B and the start of A.
-- a1: the A span start
-- a2: the A span end
-- b1: the B span start
-- b2: the B span end
-- n: the min required distance (0-N)
-- m: the max required distance (0-N)

CREATE OR REPLACE FUNCTION pyt_is_after_within(a1 INT, a2 INT, b1 INT, b2 INT,
  n INT, m INT)
RETURNS BOOLEAN AS $$
  SELECT a1 > b2 AND a1 - b2 - 1 BETWEEN n AND m;
$$
LANGUAGE sql IMMUTABLE PARALLEL SAFE;

-----------------------------------------------------------
-- pyt_is_near_within
-- Determines whether span A is near to span B within the specified distance,
-- i.e. either before or after it.
-- a1: the A span start
-- a2: the A span end
-- b1: the B span start
-- b2: the B span end
-- n: the min required distance (0-N)
-- m: the max required distance (0-N)

CREATE OR REPLACE FUNCTION pyt_is_near_within(a1 INT, a2 INT, b1 INT, b2 INT,
  n INT, m INT)
RETURNS BOOLEAN AS $$
  SELECT (a2 < b1 AND b1 - a2 - 1 BETWEEN n AND m)
    OR (a1 > b2 AND a1 - b2 - 1 BETWEEN n AND m);
$$
LANGUAGE sql IMMUTABLE PARALLEL SAFE;

-----------------------------------------------------------
-- pyt_is_left_aligned
-- Determines whether span A is left aligned to span B within the
-- specified distance. A must start with or after B, but not before.
-- a1: the A span start
-- b1: the B span start
-- n: the min required distance (0-N)
-- m: the max required distance (0-N)

CREATE OR REPLACE FUNCTION pyt_is_left_aligned(a1 INT, b1 INT, n INT, m INT)
  RETURNS BOOLEAN AS $$
  SELECT a1 >= b1 AND a1 - b1 BETWEEN n AND m;
$$
LANGUAGE sql IMMUTABLE PARALLEL SAFE;

-----------------------------------------------------------
-- pyt_is_right_aligned
-- Determines whether span A is right aligned to span B within the
-- specified distance. A must end with or before B, but not after.
-- a2: the A span end
-- b2: the B span end
-- n: the min required distance (0-N)
-- m: the max required distance (0-N)

CREATE OR REPLACE FUNCTION pyt_is_right_aligned(a2 INT, b2 INT, n INT, m INT)
  RETURNS BOOLEAN AS $$
  SELECT a2 <= b2 AND b2 - a2 BETWEEN n AND m;
$$
LANGUAGE sql IMMUTABLE PARALLEL SAFE;
