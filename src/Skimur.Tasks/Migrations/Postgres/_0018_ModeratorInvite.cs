﻿using ServiceStack.OrmLite.Dapper;
using Skimur.Data;
using Skimur.Postgres.Migrations;

namespace Skimur.Tasks.Migrations.Postgres
{
    // ReSharper disable once InconsistentNaming
    public class _0018_ModeratorInvite : Migration
    {
        public _0018_ModeratorInvite() : base(MigrationType.Schema, 18)
        {

        }

        public override void Execute(IDbConnectionProvider conn)
        {
            conn.Perform(x =>
            {
                x.Execute(@"
CREATE TABLE moderator_invites
(
  id uuid NOT NULL,
  user_id uuid NOT NULL,
  sub_id uuid NOT NULL,
  invited_by uuid NULL,
  invited_on timestamp without time zone NOT NULL,
  permissions integer NOT NULL,
  CONSTRAINT moderator_invites_pkey PRIMARY KEY (id)
);");
            });
        }

        public override string GetDescription()
        {
            return "Add table to store pending mod invites.";
        }
    }
}
