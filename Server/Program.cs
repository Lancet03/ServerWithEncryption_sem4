using DatabaseConnLibrary;
using EncryptionLibrary;
using System.Numerics;

class Program
{
    static async Task Main()
    {
        string[] args = Environment.GetCommandLineArgs();

        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration
                .AddJsonFile("config.json");
        var config = builder.Configuration;

        var app = builder.Build();

        string tableName = config?["Table"] ?? "";
        string database = config?["Database"] ?? "";
        string user = config?["User"] ?? "";
        string password = config?["Password"] ?? "";

        if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(database) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
        {
            Console.WriteLine("Отсутсвуют нужные поля в файле конфигурации");
            return;
        };

        var encryption = new RSA(200);
        var db = new DatabaseConnection(database, user, password);

        app.MapGet("/", (HttpContext context) =>
        {
            return "Health";
        });

        app.MapPost("/student", async (Student student) =>
            {
                try
                {
                    string hashedName = EncryptionLibrary.Hashing.Hash(student.Name);
                    string query = $"INSERT INTO {db.databaseName}.{tableName} (name, age, avg_score) VALUES ('{hashedName}', {student.Age}, {student.AvgScore})";

                    db.InsertInTable(query);
                    return "Success";
                }
                catch (Exception ex)
                {
                    return $"Error inserting data: {ex.Message}";
                }
            });

        app.MapGet("/student", (HttpContext context) =>
        {
            string name = context.Request.Query["name"];
            Student student = null;

            var table = db.RunSelectQuery($"SELECT * FROM {db.databaseName}.{tableName} WHERE name = '{name}'");

            if (table.Rows.Count != 0)
            {
                var row = table.Rows[0];
                student = new Student();
                student.Name = row["name"].ToString();
                student.Age = Convert.ToInt32(row["age"]);
                student.AvgScore = Convert.ToInt32(row["avg_score"]);
            }


            List<BigInteger> encryptedResp;
            if (student == null)
            {
                encryptedResp = encryption.EncryptString("Такого студента нет!");
            }
            else
            {
                string response = $"{student.Name};{student.Age};{student.AvgScore}";
                encryptedResp = encryption.EncryptString(response);
            }

            return string.Join(" ", encryptedResp);
        });

        app.Run();
    }

    public class Student
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public double AvgScore { get; set; }
    }
}
