using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Dapper;
using MySql.Data.MySqlClient;
using SourceAFIS;

namespace FingerprintMatching
{
    public record Subject(Int32 id, String name, byte[] template);

    class Program
    {
        static MySqlConnection connection;

        static void connectToDatabase() {

            connection = new MySqlConnection(
                "server=localhost;user=root;database=learn;port=3306;password="
            );
            connection.Open();
        }

        static void Main(string[] args)
        {
            connectToDatabase();

            var timer = new Stopwatch();
            timer.Start();

            var probe = new FingerprintTemplate(
                new FingerprintImage(
                    File.ReadAllBytes("samples/1__M_Right_ring_finger_CR.BMP")
                )
            );

            var subjects = connection.Query<Subject>(
                "SELECT id, name, template FROM subjects;"
            );

            Console.WriteLine("Count : " + subjects.AsList().Count);
            Console.WriteLine(identify(probe, subjects));

            timer.Stop();
            Console.WriteLine("Running Time : " + timer.ElapsedMilliseconds + " ms");
        }

        static Subject identify(FingerprintTemplate probe, IEnumerable<Subject> candidates) {
            
            var matcher = new FingerprintMatcher(probe);
            Subject match = null;
            double max = Double.NegativeInfinity;

            foreach (var candidate in candidates)
            {
                double similarity = matcher.Match(new FingerprintTemplate(candidate.template));

                if (similarity > max) {

                    max = similarity;
                    match = candidate;
                }
            }

            double threshold = 40;
            return max >= threshold ? match : null;
        }

        static void registerSamples() {

            foreach(String file in Directory.EnumerateFiles("samples")) {

                FingerprintTemplate template = new FingerprintTemplate(
                    new FingerprintImage(File.ReadAllBytes(file))
                );

                String name = Faker.Name.FullName(Faker.NameFormats.WithPrefix);
                byte[] serialized = template.ToByteArray();

                MySqlCommand command = connection.CreateCommand();

                command.CommandText = "INSERT INTO subjects (name, filename, template) VALUES ( @name, @filename, @template )";

                command.Parameters.Add("@name", MySqlDbType.VarChar).Value = name;
                command.Parameters.Add("@filename", MySqlDbType.VarChar).Value = file;
                command.Parameters.Add("@template", MySqlDbType.VarBinary).Value = serialized;

                command.ExecuteNonQuery();

                Console.WriteLine("Successfully created a new subject.");
            }
        }
    }
}
