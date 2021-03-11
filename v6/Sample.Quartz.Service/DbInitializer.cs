using System;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.FileProviders;

namespace Sample.Quartz.Service
{
    internal class DbInitializer
    {
        public static void InitializeDb(string connectionString)
        {
            string sql = "";
            var embeddedProvider = new EmbeddedFileProvider(Assembly.GetEntryAssembly());
            var fileInfo = embeddedProvider.GetFileInfo("quartz_table_sqlite.sql");
            using(var stream = fileInfo.CreateReadStream())
            using (var reader = new StreamReader(stream))
            {
                sql = reader.ReadToEnd();
            }
             
            using (var connection = new SqliteConnection(connectionString))
            {
                var dbFilePath = connection.DataSource;
                if (!File.Exists(dbFilePath))
                {
                    connection.Open();

                    var command = connection.CreateCommand();
                    command.CommandText = sql;
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Cannot Generate Database @ {0}", dbFilePath);
                        Console.WriteLine("  Error : {0}", e);
                        throw;
                    }
                    
                }
            }
        }
    }
}