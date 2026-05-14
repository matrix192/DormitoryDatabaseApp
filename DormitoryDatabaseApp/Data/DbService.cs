using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Npgsql;
using System.Data;

namespace DormitoryDatabaseApp.Data
{
    public class DbService
    {
        private readonly string _connectionString;

        public DbService()
        {
            _connectionString =
                "Host=localhost;" +
                "Port=5432;" +
                "Database=obshgiBSU;" +
                "Username=postgres;" +
                "Password=1337;";
        }

        public DataTable GetAll(string tableName)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            string query = $@"SELECT * FROM ""{tableName}"" ORDER BY 1";

            using var adapter = new NpgsqlDataAdapter(query, connection);
            var table = new DataTable();
            adapter.Fill(table);

            return table;
        }

        public List<string> GetColumns(string tableName)
        {
            var columns = new List<string>();

            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            string query = @"
                SELECT column_name
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = @tableName
                ORDER BY ordinal_position;
            ";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@tableName", tableName);

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                columns.Add(reader.GetString(0));
            }

            return columns;
        }

        public void Insert(string tableName, Dictionary<string, object?> values)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            var columns = values.Keys.ToList();

            string columnList = string.Join(", ", columns.Select(c => $@"""{c}"""));
            string parameterList = string.Join(", ", columns.Select(c => $"@{NormalizeParameterName(c)}"));

            string query = $@"
                INSERT INTO ""{tableName}"" ({columnList})
                VALUES ({parameterList});
            ";

            using var command = new NpgsqlCommand(query, connection);

            foreach (var pair in values)
            {
                command.Parameters.AddWithValue(
                    $"@{NormalizeParameterName(pair.Key)}",
                    pair.Value ?? DBNull.Value
                );
            }

            command.ExecuteNonQuery();
        }

        public void Update(string tableName, string idColumn, object idValue, Dictionary<string, object?> values)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            var setParts = values.Keys
                .Where(c => c != idColumn)
                .Select(c => $@"""{c}"" = @{NormalizeParameterName(c)}")
                .ToList();

            string setClause = string.Join(", ", setParts);

            string query = $@"
                UPDATE ""{tableName}""
                SET {setClause}
                WHERE ""{idColumn}"" = @idValue;
            ";

            using var command = new NpgsqlCommand(query, connection);

            foreach (var pair in values.Where(v => v.Key != idColumn))
            {
                command.Parameters.AddWithValue(
                    $"@{NormalizeParameterName(pair.Key)}",
                    pair.Value ?? DBNull.Value
                );
            }

            command.Parameters.AddWithValue("@idValue", idValue);

            command.ExecuteNonQuery();
        }

        public void Delete(string tableName, string idColumn, object idValue)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            string query = $@"
                DELETE FROM ""{tableName}""
                WHERE ""{idColumn}"" = @idValue;
            ";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@idValue", idValue);

            command.ExecuteNonQuery();
        }

        private string NormalizeParameterName(string columnName)
        {
            return columnName
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace("ё", "е")
                .Replace("Ё", "Е");
        }
    }
}