-- trigram indexes (used for LIKE '%...%' filters)
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- profile
CREATE TABLE public.profile (
	id varchar(50) NOT NULL,
	"content" varchar NOT NULL,
	"type" varchar(50) NULL,
	user_id varchar(256) NULL,
	CONSTRAINT profile_pk PRIMARY KEY (id)
);

-- document
CREATE TABLE public."document" (
	id serial NOT NULL,
	author varchar(200) NOT NULL,
	title varchar(500) NOT NULL,
	"source" varchar(500) NOT NULL,
	profile_id varchar(50) NOT NULL,
	user_id varchar(256) NULL,
	date_value float8 NOT NULL,
	sort_key varchar(500) NOT NULL,
	last_modified timestamptz NOT NULL,
	content varchar NULL,
	CONSTRAINT document_pk PRIMARY KEY (id)
);
CREATE INDEX document_source_idx ON public.document USING btree (source);
CREATE INDEX document_user_id_idx ON public.document USING btree (user_id);
CREATE INDEX document_last_modified_idx ON public."document" (last_modified);
-- sorted paging of documents: one index per sort order, plus its mirror for
-- the descending order, where only the first key is descending (a backward
-- scan of the mirror yields e.g. author DESC, title, date_value, id)
CREATE INDEX document_author_sort_idx ON public.document USING btree (author, title, date_value, id);
CREATE INDEX document_author_sort_desc_idx ON public.document USING btree (author, title DESC, date_value DESC, id DESC);
CREATE INDEX document_title_sort_idx ON public.document USING btree (title, author, date_value, id);
CREATE INDEX document_title_sort_desc_idx ON public.document USING btree (title, author DESC, date_value DESC, id DESC);
CREATE INDEX document_date_value_sort_idx ON public.document USING btree (date_value, author, title, id);
CREATE INDEX document_date_value_sort_desc_idx ON public.document USING btree (date_value, author DESC, title DESC, id DESC);
CREATE INDEX document_sort_key_id_idx ON public.document USING btree (sort_key, id);
-- profile: FK, equality and prefix (LIKE 'x%') lookups
CREATE INDEX document_profile_id_idx ON public.document USING btree (profile_id varchar_pattern_ops);
-- case-insensitive equality on privileged attributes (search: LOWER(x)=LOWER('...'))
CREATE INDEX document_author_lower_idx ON public.document USING btree (LOWER(author));
CREATE INDEX document_title_lower_idx ON public.document USING btree (LOWER(title));
CREATE INDEX document_source_lower_idx ON public.document USING btree (LOWER(source));
CREATE INDEX document_profile_id_lower_idx ON public.document USING btree (LOWER(profile_id));
CREATE INDEX document_sort_key_lower_idx ON public.document USING btree (LOWER(sort_key));
-- case-insensitive substring filters (LOWER(x) LIKE '%...%')
CREATE INDEX document_author_lower_trgm_idx ON public.document USING gin (LOWER(author) gin_trgm_ops);
CREATE INDEX document_title_lower_trgm_idx ON public.document USING gin (LOWER(title) gin_trgm_ops);
CREATE INDEX document_source_lower_trgm_idx ON public.document USING gin (LOWER(source) gin_trgm_ops);
ALTER TABLE public."document" ADD CONSTRAINT document_fk FOREIGN KEY (profile_id) REFERENCES profile(id) ON DELETE CASCADE ON UPDATE CASCADE;

-- document_attribute
CREATE TABLE document_attribute (
	id serial NOT NULL,
	document_id int4 NOT NULL,
	"name" varchar(100) NOT NULL,
	value varchar(500) NOT NULL,
	"type" int4 NOT NULL,
	CONSTRAINT document_attribute_pk PRIMARY KEY (id)
);
-- FK, attributes of a document, and attribute of a document by name
CREATE INDEX document_attribute_document_id_name_idx ON public.document_attribute (document_id, name);
-- exact name (and value) lookups, also serving DISTINCT name (word index)
CREATE INDEX document_attribute_name_value_idx ON public.document_attribute USING btree (name, value, document_id);
CREATE INDEX document_attribute_value_idx ON public.document_attribute USING btree (value);
-- case-insensitive name (and value) lookups (search and documents filters)
CREATE INDEX document_attribute_name_value_lower_idx ON public.document_attribute USING btree (LOWER(name), LOWER(value), document_id);
-- case-insensitive substring filters on values and names
CREATE INDEX document_attribute_value_lower_trgm_idx ON public.document_attribute USING gin (LOWER(value) gin_trgm_ops);
CREATE INDEX document_attribute_name_lower_trgm_idx ON public.document_attribute USING gin (LOWER(name) gin_trgm_ops);
ALTER TABLE public.document_attribute ADD CONSTRAINT document_attribute_fk FOREIGN KEY (document_id) REFERENCES "document"(id) ON DELETE CASCADE ON UPDATE CASCADE;

-- public.corpus
CREATE TABLE public.corpus (
	id varchar(50) NOT NULL,
	title varchar(100) NOT NULL,
	description varchar(1000) NOT NULL,
	user_id varchar(256) NULL,
	CONSTRAINT corpus_pk PRIMARY KEY (id)
);

-- public.document_corpus
CREATE TABLE public.document_corpus (
	document_id int NOT NULL,
	corpus_id varchar(50) NOT NULL,
	CONSTRAINT document_corpus_pk PRIMARY KEY (document_id,corpus_id),
	CONSTRAINT document_corpus_fk_d FOREIGN KEY (document_id) REFERENCES public."document"(id) ON DELETE CASCADE ON UPDATE CASCADE,
	CONSTRAINT document_corpus_fk_c FOREIGN KEY (corpus_id) REFERENCES public.corpus(id) ON DELETE CASCADE ON UPDATE CASCADE
);
CREATE INDEX corpus_title_idx ON public.corpus (title);
-- documents of a corpus (the PK only serves corpora of a document)
CREATE INDEX document_corpus_corpus_id_idx ON public.document_corpus (corpus_id, document_id);

-- AUTH

-- public.app_user definition
CREATE TABLE app_user (
	id text NOT NULL,
	first_name text NULL,
	last_name text NULL,
	user_name varchar(256) NULL,
	normalized_user_name varchar(256) NULL,
	email varchar(256) NULL,
	normalized_email varchar(256) NULL,
	email_confirmed bool NOT NULL,
	password_hash text NULL,
	security_stamp text NULL,
	concurrency_stamp text NULL,
	phone_number text NULL,
	phone_number_confirmed bool NOT NULL,
	two_factor_enabled bool NOT NULL,
	lockout_end timestamptz NULL,
	lockout_enabled bool NOT NULL,
	access_failed_count int4 NOT NULL,
	CONSTRAINT pk_app_user PRIMARY KEY (id)
);
CREATE INDEX "email_index" ON public.app_user USING btree (normalized_email);
CREATE UNIQUE INDEX "user_name_index" ON public.app_user USING btree (normalized_user_name);

-- public.app_role definition
CREATE TABLE app_role (
	id text NOT NULL,
	"name" varchar(256) NULL,
	normalized_name varchar(256) NULL,
	concurrency_stamp text NULL,
	CONSTRAINT pk_app_role PRIMARY KEY (id)
);
CREATE UNIQUE INDEX "role_name_index" ON public.app_role USING btree (normalized_name);

-- public."app_role_claim" definition
CREATE TABLE "app_role_claim" (
	id int4 NOT NULL GENERATED BY DEFAULT AS IDENTITY,
	role_id text NOT NULL,
	claim_type text NULL,
	claim_value text NULL,
	CONSTRAINT "pk_app_role_claim" PRIMARY KEY (id)
);
CREATE INDEX "ix_app_role_claim_role_id" ON public."app_role_claim" USING btree (role_id);
-- public."app_role_claim" foreign keys
ALTER TABLE public."app_role_claim" ADD CONSTRAINT "fk_app_role_claim_aspnetroles_role_id" FOREIGN KEY (role_id) REFERENCES app_role(id) ON DELETE CASCADE;

-- public."app_user_claim" definition
CREATE TABLE "app_user_claim" (
	id int4 NOT NULL GENERATED BY DEFAULT AS IDENTITY,
	user_id text NOT NULL,
	claim_type text NULL,
	claim_value text NULL,
	CONSTRAINT "pk_app_user_claim" PRIMARY KEY (id)
);
CREATE INDEX "ix_app_user_claim_user_id" ON public."app_user_claim" USING btree (user_id);
-- public."app_user_claim" foreign keys
ALTER TABLE public."app_user_claim" ADD CONSTRAINT "fk_app_user_claim_aspnetusers_user_id" FOREIGN KEY (user_id) REFERENCES app_user(id) ON DELETE CASCADE;

-- public."app_user_login" definition
CREATE TABLE "app_user_login" (
	login_provider text NOT NULL,
	provider_key text NOT NULL,
	provider_display_name text NULL,
	user_id text NOT NULL,
	CONSTRAINT "pk_app_user_login" PRIMARY KEY (login_provider, provider_key)
);
CREATE INDEX "ix_app_user_login_user_id" ON public."app_user_login" USING btree (user_id);
-- public."app_user_login" foreign keys
ALTER TABLE public."app_user_login" ADD CONSTRAINT "fk_app_user_login_aspnetusers_user_id" FOREIGN KEY (user_id) REFERENCES app_user(id) ON DELETE CASCADE;

-- public."app_user_role" definition
CREATE TABLE "app_user_role" (
	user_id text NOT NULL,
	role_id text NOT NULL,
	CONSTRAINT "pk_app_user_role" PRIMARY KEY (user_id, role_id)
);
CREATE INDEX "ix_app_user_role_role_id" ON public."app_user_role" USING btree (role_id);
-- public."app_user_role" foreign keys
ALTER TABLE public."app_user_role" ADD CONSTRAINT "fk_app_user_role_aspnetroles_role_id" FOREIGN KEY (role_id) REFERENCES app_role(id) ON DELETE CASCADE;
ALTER TABLE public."app_user_role" ADD CONSTRAINT "fk_app_user_role_aspnetusers_user_id" FOREIGN KEY (user_id) REFERENCES app_user(id) ON DELETE CASCADE;

-- public."app_user_token" definition
CREATE TABLE "app_user_token" (
	user_id text NOT NULL,
	login_provider text NOT NULL,
	"name" text NOT NULL,
	value text NULL,
	CONSTRAINT "pk_app_user_token" PRIMARY KEY (user_id, login_provider, name)
);
-- public."app_user_token" foreign keys
ALTER TABLE public."app_user_token" ADD CONSTRAINT "fk_app_user_token_aspnetusers_user_id" FOREIGN KEY (user_id) REFERENCES app_user(id) ON DELETE CASCADE;
