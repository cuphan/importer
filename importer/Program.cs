using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using McMaster.Extensions.CommandLineUtils;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace importer
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Product
    {
        public string sku { get; set; }
        public string description { get; set; }
        public string category { get; set; }
        public double price { get; set; }
        public string location { get; set; }
        public int qty { get; set; }
    }

    public class Transmissionsummary
    {
        public string id { get; set; }
        public int recordcount { get; set; }
        public int qtysum { get; set; }
    }

    public class Root
    {
        public List<Product> products { get; set; }
        public Transmissionsummary transmissionsummary { get; set; }
    }

    /* 
     * dotnet importer.dll -h
     * Usage: importer [options]

     * Options:
     *    -j|--json-file <JSON_FILE>  The path to json file
     *    -?|-h|--help                Show help information.
     * 
     * dotnet importer.dll -j ~/Projects/importer/importer/sample-data/drop-1.json
     * dotnet importer.dll -j ~/Projects/importer/importer/sample-data/drop-2.json
     * dotnet importer.dll -j ~/Projects/importer/importer/sample-data/drop-3.json
     */
    class Program
    {
        static void Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        [Option(Description = "The path to json file")]
        public string jsonFile { get; } = "~/sample-data/drop-1.json";

        public void insertTransmission(SqlConnection conn, Root root)
        {
            string query = "INSERT INTO transmission(id, recordcount, qtysum) VALUES(@id, @recordcount, @qtysum)";
            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", root.transmissionsummary.id);
            cmd.Parameters.AddWithValue("@recordcount", root.transmissionsummary.recordcount);
            cmd.Parameters.AddWithValue("@qtysum", root.transmissionsummary.qtysum);
            cmd.ExecuteNonQuery();
        }

        public void insertProduct(SqlConnection conn, Product product)
        {
            string query = "INSERT INTO products(sku, description, category, price, location, qty) VALUES(@sku, @description, @category, @price, @location, @qty)";
            SqlCommand cmd = new SqlCommand(query, conn);
            var category = processCategory(product.category);
            cmd.Parameters.AddWithValue("@sku", product.sku);
            cmd.Parameters.AddWithValue("@description", product.description);
            //cmd.Parameters.AddWithValue("@category", product.category);
            cmd.Parameters.AddWithValue("@category", category);
            cmd.Parameters.AddWithValue("@price", product.price);
            cmd.Parameters.AddWithValue("@location", product.location);
            cmd.Parameters.AddWithValue("@qty", product.qty);
            cmd.ExecuteNonQuery();
        }

        public bool checkTransmission(SqlConnection conn, string id)
        {
            string query = "SELECT COUNT(*) FROM transmission WHERE id='"+id+"';";
            SqlCommand cmd = new SqlCommand(query, conn);
            //cmd.Parameters.AddWithValue("@id", id);

            //Console.WriteLine("id: {0} ", id);
            if ((Int32) cmd.ExecuteScalar() > 0)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public string processCategory(string categoryString)
        {
            char[] separator = {'>'};
            var categories = categoryString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            //Console.WriteLine(categories[categories.Length - 2]);
            return categories[categories.Length - 2];
        }

        public SqlDataReader queryProduct(SqlConnection conn)
        {
            string query = "SELECT category, location, price FROM products";
            SqlCommand cmd = new SqlCommand(query, conn);
            return cmd.ExecuteReader();
        }

        private void OnExecute()
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var builder = new ConfigurationBuilder()
                    .AddJsonFile($"appsettings.json", true, true)
                    .AddEnvironmentVariables();
            var config = builder.Build();

            var productJsonString = File.ReadAllText(jsonFile);
            //Console.WriteLine(productJsonString);
            Console.WriteLine("Processing {0}", Path.GetFileName(jsonFile));

            Root root = JsonConvert.DeserializeObject<Root>(productJsonString);
            //Console.WriteLine(root.products);
            //Console.WriteLine(root.transmissionsummary);

            //var datasource = @"localhost";//your server
            //var database = "DevOpsDB"; //your database name
            //var username = "sa"; //username of server to connect
            //var password = "p@ssw0rd"; //password

            //your connection string 
            //string connString = @"Data Source=" + datasource + ";Initial Catalog=" + database + ";Persist Security Info=True;User ID=" + username + ";Password=" + password;
            //var connString = config["ConnectionString"];

            //Console.WriteLine("ConnectionString: {0}", config["ConnectionString"]);
            SqlConnection conn = new SqlConnection(config["ConnectionString"]);

            try
            {                
                conn.Open();

                // check if transmissionsummary.id not in the table
                if (checkTransmission(conn, root.transmissionsummary.id) == false) {
                    // insert to transmission table
                    insertTransmission(conn, root);

                    // insert to products table
                    foreach (var product in root.products)
                    {
                        insertProduct(conn, product);
                    }

                    //query product
                    SqlDataReader reader = queryProduct(conn);

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            var category = reader.GetString(0);
                            var location = reader.GetString(1);
                            var price = reader.GetDecimal(2).ToString();
                            
                            Console.WriteLine("{0} - {1} - {2}", category, location, price);
                        }
                    }
                    else
                    {
                        Console.WriteLine("No data found.");
                    }
                }                
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }

            Console.WriteLine("Completed {0}", Path.GetFileName(jsonFile));
        }
    }
}
