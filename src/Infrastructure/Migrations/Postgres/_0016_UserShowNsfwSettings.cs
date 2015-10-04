﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.Data;
using Infrastructure.Postgres.Migrations;
using ServiceStack.OrmLite.Dapper;

namespace Migrations.Postgres
{
    public class _0016_UserShowNsfwSettings : Migration
    {
        public _0016_UserShowNsfwSettings() : base(MigrationType.Schema, 16)
        {

        }


        public override void Execute(IDbConnectionProvider conn)
        {
            conn.Perform(x =>
            {
                x.Execute("ALTER TABLE users ADD COLUMN show_nsfw boolean NOT NULL default FALSE;");
            });
        }

        public override string GetDescription()
        {
            return "Add a field to the user table to indicate if the user wants to show nsfw content.";
        }
    }
}
