﻿using System.Data;
using ServiceStack.OrmLite.Dapper;
using Skimur.Data;
using Skimur.Postgres.Migrations;

namespace Skimur.Tasks.Migrations.Postgres
{
    // ReSharper disable once InconsistentNaming
    public class _0001_InitialSchema : Migration
    {
        public _0001_InitialSchema() : base(MigrationType.Schema, 1)
        {

        }


        public override void Execute(IDbConnectionProvider conn)
        {
            conn.Perform(x =>
            {
                CreateFunctions(x);
                CreateTables(x);
            });
        }

        public override string GetDescription()
        {
            return "Create the initial schema.";
        }

        private void CreateFunctions(IDbConnection conn)
        {
            conn.Execute(@"
CREATE OR REPLACE FUNCTION clear_database() RETURNS void AS $$
DECLARE
BEGIN
    DELETE FROM votes;
	DELETE FROM votes;
	DELETE FROM posts;
	DELETE FROM comments;
	DELETE FROM messages;
	DELETE FROM sub_scriptions;
	DELETE FROM sub_admins;
	DELETE FROM sub_user_bans;
	DELETE FROM subs;
	DELETE FROM user_logins;
	DELETE FROM user_roles;
	DELETE FROM roles;
	DELETE from users;
END;
$$ LANGUAGE plpgsql;");

            conn.Execute(@"
CREATE OR REPLACE FUNCTION ensure_default_user_exists() RETURNS void AS $$
DECLARE
BEGIN

IF NOT EXISTS (SELECT 1 FROM users WHERE user_name = 'skimur') 
THEN
  INSERT INTO users (id, created_date, user_name, password_hash) values ('81e2d7a7-1e56-e511-84d5-5404a632cbf7', '2015-09-08 11:42:17.961907+00', 'skimur', 'AEX8cmLa7jBdhKaG5Gh8I5IIlWF6GBc5pmcED5QIdRZ45qOwky5GhvXkCTYo3Aey7A==');
END IF;

END;
$$ LANGUAGE plpgsql;
");

            conn.Execute(@"
create or replace function hot(ups integer, downs integer, date timestamp with time zone) returns numeric as $$
    select round(cast(log(greatest(abs($1 - $2), 1)) * sign($1 - $2) + (date_part('epoch', $3) - 1134028003) / 45000.0 as numeric), 7)
$$ language sql immutable;");

            conn.Execute(@"
create or replace function score(ups integer, downs integer) returns integer as $$
    select $1 - $2
$$ language sql immutable;");

            conn.Execute(@"
create or replace function controversy(ups integer, downs integer) returns float as $$
    select CASE WHEN $1 <= 0 or $2 <= 0 THEN 0
                WHEN $1 > $2 THEN power($1 + $2, cast($2 as float) / $1)
                ELSE power($1 + $2, cast($1 as float) / $2)
            END;
$$ language sql immutable;");
        }

        private void CreateTables(IDbConnection conn)
        {
            conn.Execute(@"
CREATE TABLE users
(
  id uuid NOT NULL,
  created_date timestamp with time zone NOT NULL,
  user_name text NOT NULL,
  email text,
  email_confirmed boolean,
  password_hash text,
  security_stamp text,
  phone_number text,
  phone_number_confirmed boolean,
  two_factor_enabled boolean,
  lockout_end_date timestamp with time zone,
  lockout_enabled boolean,
  access_failed_count integer,
  security_question text,
  security_answer text,
  full_name text,
  bio text,
  url text,
  location text,
  avatar_identifier text,
  CONSTRAINT users_pkey PRIMARY KEY (id)
);");

            conn.Execute(@"
CREATE TABLE roles
(
  id uuid NOT NULL,
  name text NOT NULL,
  CONSTRAINT roles_pkey PRIMARY KEY (id)
);");

            conn.Execute(@"
CREATE TABLE user_roles
(
    user_id uuid NOT NULL REFERENCES users(id),
    role_id uuid NOT NULL REFERENCES roles(id)
);");

            conn.Execute(@"
CREATE TABLE user_logins
(
    id uuid NOT NULL,
    user_id uuid REFERENCES users(id),
    login_provider text NOT NULL,
    login_key text NOT NULL,
    CONSTRAINT user_logins_pkey PRIMARY KEY(id)
);");

            conn.Execute(@"
CREATE TABLE subs
(
    id uuid NOT NULL,
    created_date timestamp with time zone NOT NULL,
    name text,
    description text,
    sidebar_text text,
    is_default boolean,
    number_of_subscribers bigint,
    type integer,
    CONSTRAINT subs_pkey PRIMARY KEY(id)
);");

            conn.Execute(@"
CREATE TABLE sub_admins
(
    id uuid NOT NULL,
    user_id uuid,
    sub_id uuid,
    added_by uuid,
    added_on timestamp with time zone,
    CONSTRAINT sub_admins_pkey PRIMARY KEY(id)
);");

            conn.Execute(@"
CREATE TABLE sub_scriptions
(
    id uuid NOT NULL,
    user_id uuid,
    sub_id uuid,
    CONSTRAINT sub_scriptions_pkey PRIMARY KEY(id)
);");


            conn.Execute(@"
CREATE TABLE posts
(
    id uuid NOT NULL,
    date_created timestamp with time zone NOT NULL,
    last_edit_date timestamp with time zone,
    slug text,
    sub_id uuid,
    user_id uuid,
    user_ip text,
    type integer,
    title text,
    content text,
    url text,
    domain text,
    send_replies boolean,
    vote_up_count integer,
    vote_down_count integer,
    CONSTRAINT post_pkey PRIMARY KEY(id)
);");


            conn.Execute(@"
CREATE INDEX posts_hot_index ON posts(hot(vote_up_count, vote_down_count, date_created), date_created);
CREATE INDEX posts_score_index ON posts(score(vote_up_count, vote_down_count), date_created);
CREATE INDEX posts_controversy_index ON posts(controversy(vote_up_count, vote_down_count), date_created);");

            conn.Execute(@"
CREATE TABLE votes
(
    id uuid NOT NULL,
    date_created timestamp with time zone,
    user_id uuid,
    post_id uuid,
    comment_id uuid,
    type integer,
    date_casted timestamp with time zone,
    ip_address text,
    user_ip text,
    CONSTRAINT votes_pkey PRIMARY KEY(id)
);");

            conn.Execute(@"
CREATE TABLE comments
(
  id uuid NOT NULL,
  date_created timestamp with time zone,
  date_edited timestamp with time zone,
  sub_id uuid,
  parent_id uuid,
  parents text,
  author_user_id uuid,
  author_ip_address text,
  post_id uuid,
  body text,
  body_formatted text,
  send_replies boolean,
  vote_up_count integer,
  vote_down_count integer,
  deleted boolean,
  sort_confidence numeric,
  sort_qa numeric,
  CONSTRAINT comments_pkey PRIMARY KEY (id)
);");

            conn.Execute(@"
CREATE INDEX comments_score_index ON comments(hot(vote_up_count, vote_down_count, date_created), date_created);
CREATE INDEX comments_controversy_index ON comments(controversy(vote_up_count, vote_down_count), date_created);");

            conn.Execute(@"
CREATE TABLE sub_user_bans
(
    id uuid NOT NULL,
    sub_id uuid,
    user_id uuid,
    user_name text,
    banned_until timestamp with time zone,
    date_banned timestamp with time zone,
    banned_by uuid,
    reason_private text,
    reason_public text,
    CONSTRAINT sub_user_bans_pkey PRIMARY KEY(id)
);");
        }
    }
}
