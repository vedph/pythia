-----------------------------------------------------------
-- boolean pyt_is_numeric(text)
-- Rets true if the specified text is a valid numeric type.
-- https://stackoverflow.com/questions/16195986/isnumeric-with-postgresql

CREATE OR REPLACE FUNCTION pyt_is_numeric(text) RETURNS BOOLEAN AS $$
DECLARE
  x NUMERIC;
BEGIN
  x = $1::numeric;
  return true;
exception when others then
  return false;
END;
$$
STRICT
LANGUAGE plpgsql IMMUTABLE;

-----------------------------------------------------------
-- int pyt_min
-- Returns the minimum between two integers.
-- a: the first integer.
-- b: the second integer.

CREATE OR REPLACE FUNCTION public.pyt_min(a INT, b INT)	RETURNS INT AS $$
	BEGIN
    if a <= b then
    	return a;
    end if;
    return b;
	END;
$$
STRICT
LANGUAGE plpgsql IMMUTABLE;

-----------------------------------------------------------
-- int pyt_max
-- Returns the maximum between two integers.
-- a: the first integer.
-- b: the second integer.

CREATE OR REPLACE FUNCTION public.pyt_max(a INT, b INT) RETURNS INT AS $$
	BEGIN
    if a >= b then
    	return a;
    end if;
    return b;
	END;
$$
STRICT
LANGUAGE plpgsql IMMUTABLE;

-----------------------------------------------------------
-- boolean pyt_is_overlap
-- Determines whether spans A and B overlap.
-- a1: the A span start
-- a2: the A span end
-- b1: the B span start
-- b2: the B span end

CREATE OR REPLACE FUNCTION pyt_is_overlap(a1 INT, a2 INT, b1 INT, b2 INT)
RETURNS BOOLEAN AS $$
DECLARE
  min1 INT;
  max2 INT;
BEGIN
  -- int min1 = math.min(a1, b1);
  -- int max2 = math.max(a2, b2);
  -- for (int i = min1; i <= max2; i++)
  -- {
  --     if (i >= a1 && i <= a2 && i >= b1 && i <= b2) return true;
  -- }
  -- return false;
  min1 = pyt_min(a1, b1);
  max2 = pyt_max(a2, b2);
  for i in min1..max2 loop
    if (i >= a1) and (i <= a2) and (i >= b1) and (i <= b2) then
      return true; 
    end if;
  end loop;
  return false;
END;
$$
STRICT
LANGUAGE plpgsql IMMUTABLE;

-----------------------------------------------------------
-- boolean pyt_get_overlap_count
-- Gets the count of items of span A overlapping with items of span B.
-- a1: the A span start
-- a2: the A span end
-- b1: the B span start
-- b2: the B span end

CREATE OR REPLACE FUNCTION pyt_get_overlap_count(a1 INT, a2 INT, b1 INT, b2 INT)
RETURNS INT AS $$
DECLARE
  min1 INT;
  max2 INT;
  n INT;
BEGIN
  -- int min = Math.Min(a1, b1);
  -- int max = Math.Max(a2, b2);
  -- int n = 0;
  -- for (int i = min; i <= max; i++)
  -- {
  --     if (i >= a1 && i <= a2 && i >= b1 && i <= b2) n++;
  -- }
  -- return n;
  min1 = pyt_min(a1, b1);
  max2 = pyt_max(a2, b2);
  n = 0;
  for i in min1..max2 loop
    if (i >= a1) and (i <= a2) and (i >= b1) and (i <= b2) then
      n = n + 1;
    end if;
  end loop;
  return n;
END;
$$
STRICT
LANGUAGE plpgsql IMMUTABLE;

-----------------------------------------------------------
-- boolean pyt_is_overlap_within
-- Determines whether spans A and B overlap within the specified extent.
-- a1: the A span start
-- a2: the A span end
-- b1: the B span start
-- b2: the B span end
-- n: the minimum required overlap count (1-N)
-- m: the maximum required overlap count (1-M)

CREATE OR REPLACE FUNCTION pyt_is_overlap_within(a1 INT, a2 INT, b1 INT, b2 INT, n INT, m INT)
RETURNS BOOLEAN AS $$
DECLARE
  min1 INT;
  max2 INT;
  d INT;
BEGIN
  -- // if a before b or a after b, no overlap
  -- if (a2 < b1 || a1 > b2) return false;
  -- int d = GetOverlapCount(a1, a2, b1, b2);
  -- return d >= n && d <= m;
  if (a2 < b1) or (a1 > b2) then
    return false;
  end if;
  d = pyt_get_overlap_count(a1, a2, b1, b2);
  return (d >= n) and (d <= m); 
END;
$$
STRICT
LANGUAGE plpgsql IMMUTABLE;

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
DECLARE
  ds INT;
  de INT;
BEGIN
  -- if (a1 < b1 || a2 > b2) return false;
  -- int ds = a1 - b1;
  -- int de = b2 - a2;
  -- return ds >= ns && ds <= ms && de >= ne && de <= me;
  if (a1 < b1) or (a2 > b2) then
    return false;
  end if;
  ds = a1 - b1;
  de = b2 - a2;
  return (ds >= ns) and (ds <= ms) and (de >= ne) and (de <= me);
END;
$$
STRICT
LANGUAGE plpgsql IMMUTABLE;

-----------------------------------------------------------
-- pyt_is_before_within
-- Determines whether span A is before span B within the specified distance.
-- a1: the A span start
-- a2: the A span end
-- b1: the B span start
-- b2: the B span end
-- n: the min required distance (0-N)
-- m: the max required distance (0-N)
CREATE OR REPLACE FUNCTION pyt_is_before_within(a1 INT, a2 INT, b1 INT, b2 INT,
  n INT, m INT)
RETURNS BOOLEAN AS $$
DECLARE
  d INT;
BEGIN
  -- if (a2 >= b1 || IsOverlap(a1, a2, b1, b2)) return false;
  -- int d = b1 - a2 - 1;
  -- return d >= n && d <= m;

  -- a is before b when a2 < b1
  if (a2 >= b1) or (pyt_is_overlap(a1, a2, b1, b2)) then
    return false;
  end if;
  d = b1 - a2 - 1;
  return (d >= n) and (d <= m);
END;
$$
STRICT
LANGUAGE plpgsql IMMUTABLE;

-----------------------------------------------------------
-- pyt_is_after_within
-- Determines whether span A is after span B within the specified distance.
-- a1: the A span start
-- a2: the A span end
-- b1: the B span start
-- b2: the B span end
-- n: the min required distance (0-N)
-- m: the max required distance (0-N)

CREATE OR REPLACE FUNCTION pyt_is_after_within(a1 INT, a2 INT, b1 INT, b2 INT,
  n INT, m INT)
RETURNS BOOLEAN AS $$
DECLARE
  d INT;
BEGIN
  -- a is after b when a1 > b2
  -- if (a1 <= b2 || isoverlap(a1, a2, b1, b2)) return false;
  -- int d = a1 - b2 - 1;
  -- return d >= n && d <= m;

  if (a1 <= b2) or (pyt_is_overlap(a1, a2, b1, b2)) then
    return false;
  end if;
  d = a1 - b2 - 1;
  return (d >= n) and (d <= m);
END;
$$
STRICT
LANGUAGE plpgsql IMMUTABLE;

-----------------------------------------------------------
-- pyt_is_near_within
-- Determines whether span A is near to span B within the specified distance.
-- a1: the A span start
-- a2: the A span end
-- b1: the B span start
-- b2: the B span end
-- n: the min required distance (0-N)
-- m: the max required distance (0-N)

CREATE OR REPLACE FUNCTION pyt_is_near_within(a1 INT, a2 INT, b1 INT, b2 INT,
  n INT, m INT)
RETURNS BOOLEAN AS $$
BEGIN
  -- return IsBeforeWithin(a1, a2, b1, b2, n, m)
  -- || IsAfterWithin(a1, a2, b1, b2, n, m);

  return (pyt_is_before_within(a1, a2, b1, b2, n, m))
    or (pyt_is_after_within(a1, a2, b1, b2, n, m));
END;
$$
STRICT
LANGUAGE plpgsql IMMUTABLE;

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
BEGIN
  -- return a1 - b1 >= n && a1 - b1 <= m;
  return (a1 - b1 >= n) and (a1 - b1 <= m);
END;
$$
STRICT
LANGUAGE plpgsql IMMUTABLE;

-----------------------------------------------------------
-- pyt_is_right_aligned
-- Determines whether span A is right aligned to span B within the
-- specified distance. A must end with or before B, but not after.
-- a2: the A span start
-- b2: the B span start
-- n: the min required distance (0-N)
-- m: the max required distance (0-N)

CREATE OR REPLACE FUNCTION pyt_is_right_aligned(a2 INT, b2 INT, n INT, m INT)
  RETURNS BOOLEAN AS $$
BEGIN
  -- return b2 - a2 >= n && b2 - a2 <= m;
  return (b2 - a2 >= n) and (b2 - a2 <= m);
END;
$$
STRICT
LANGUAGE plpgsql IMMUTABLE;
