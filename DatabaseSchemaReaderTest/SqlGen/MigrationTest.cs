﻿using System;
using System.Data.Common;
using System.Transactions;
using DatabaseSchemaReader.DataSchema;
using DatabaseSchemaReader.SqlGen;
using DatabaseSchemaReader.Utilities;
#if !NUNIT
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
using TestInitialize = NUnit.Framework.SetUpAttribute;
using TestCleanup = NUnit.Framework.TearDownAttribute;
using TestContext = System.Object;
#endif

namespace DatabaseSchemaReaderTest.SqlGen
{
    [TestClass]
    public class MigrationTest
    {
        private const string ProviderName = "System.Data.SqlClient";
        private const string ConnectionString = @"Data Source=.\SQLEXPRESS;Integrated Security=true;Initial Catalog=Northwind";
        private readonly DbProviderFactory _factory;

        public MigrationTest()
        {
            _factory = DbProviderFactories.GetFactory(ProviderName);
        }

        [TestMethod]
        public void TestMigration()
        {
            //arrange
            var tableName = MigrationCommon.FindFreeTableName(ProviderName, ConnectionString);
            var migration = new DdlGeneratorFactory(SqlType.SqlServer).MigrationGenerator();

            var table = MigrationCommon.CreateTestTable(tableName);
            var newColumn = MigrationCommon.CreateNewColumn();
            var unqiueConstraint = MigrationCommon.CreateUniqueConstraint(newColumn);
            var fk = MigrationCommon.CreateForeignKey(table);

            var createTable = migration.AddTable(table);
            var addColumn = migration.AddColumn(table, newColumn);
            var addUniqueConstraint = migration.AddConstraint(table, unqiueConstraint);
            var addForeignKey = migration.AddConstraint(table, fk);

            var dropForeignKey = migration.DropConstraint(table, fk);
            var dropUniqueConstraint = migration.DropConstraint(table, unqiueConstraint);
            var dropColumn = migration.DropColumn(table, newColumn);
            var dropTable = migration.DropTable(table);
            var statements = ScriptTools.SplitScript(createTable);


            //we need to strip out the "GO" parts from these scripts

            using (new TransactionScope())
            {
                using (var con = _factory.CreateConnection())
                {
                    con.ConnectionString = ConnectionString;
                    using (var cmd = con.CreateCommand())
                    {
                        con.Open();
                        foreach (var statement in statements)
                        {
                            //ignore the drop table bit, which has no useful commands
                            if (statement.Contains(Environment.NewLine + "-- DROP TABLE")) continue;
                            Console.WriteLine("Executing " + statement);
                            cmd.CommandText = statement;
                            cmd.ExecuteNonQuery();
                        }

                        foreach (var statement in ScriptTools.SplitScript(addColumn))
                        {
                            Console.WriteLine("Executing " + statement);
                            cmd.CommandText = statement;
                            cmd.ExecuteNonQuery();
                        }

                        foreach (var statement in ScriptTools.SplitScript(addUniqueConstraint))
                        {
                            Console.WriteLine("Executing " + statement);
                            cmd.CommandText = statement;
                            cmd.ExecuteNonQuery();
                        }

                        foreach (var statement in ScriptTools.SplitScript(addForeignKey))
                        {
                            Console.WriteLine("Executing " + statement);
                            cmd.CommandText = statement;
                            cmd.ExecuteNonQuery();
                        }


                        foreach (var statement in ScriptTools.SplitScript(dropForeignKey))
                        {
                            Console.WriteLine("Executing " + statement);
                            cmd.CommandText = statement;
                            cmd.ExecuteNonQuery();
                        }

                        foreach (var statement in ScriptTools.SplitScript(dropUniqueConstraint))
                        {
                            Console.WriteLine("Executing " + statement);
                            cmd.CommandText = statement;
                            cmd.ExecuteNonQuery();
                        }

                        foreach (var statement in ScriptTools.SplitScript(dropColumn))
                        {
                            Console.WriteLine("Executing " + statement);
                            cmd.CommandText = statement;
                            cmd.ExecuteNonQuery();
                        }

                        foreach (var statement in ScriptTools.SplitScript(dropTable))
                        {
                            Console.WriteLine("Executing " + statement);
                            cmd.CommandText = statement;
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }
    }
}
