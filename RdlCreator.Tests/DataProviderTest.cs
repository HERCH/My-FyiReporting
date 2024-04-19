﻿using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System;
using RdlCreator;
using NUnit.Framework;
using System.Text.RegularExpressions;

namespace RdlCreator.Tests
{
    [TestFixture]
    public class DataProviderTest
    {
        string connectionString = "Data Source=sqlitetestdb2.db;";
        string dataProvider = "Microsoft.Data.Sqlite";

        [Test]
        public void TestMethod1()
        {
            var create = new RdlCreator.Create();
           
            var fyiReport = create.GenerateRdl(dataProvider, 
                connectionString, 
                "SELECT CategoryID, CategoryName, Description FROM Categories",
                pageHeaderText: "DataProviderTest TestMethod1");
            var ms = new fyiReporting.RDL.MemoryStreamGen();
            fyiReport.RunGetData(null);
            fyiReport.RunRender(ms, fyiReporting.RDL.OutputPresentationType.CSV);
            var text = ms.GetText();
           
            Assert.That(text, Is.Not.Null);
            Assert.That(NormalizeEOL(text), Is.EqualTo(@"""DataProviderTest TestMethod1""
""CategoryID"",""CategoryName"",""Description""
1,""Beverages"",""Soft drinks, coffees, teas, beers, and ales""
2,""Condiments"",""Sweet and savory sauces, relishes, spreads, and seasonings""
3,""Confections"",""Desserts, candies, and sweet breads""
4,""Dairy Products"",""Cheeses""
5,""Grains/Cereals"",""Breads, crackers, pasta, and cereal""
6,""Meat/Poultry"",""Prepared meats""
7,""Produce"",""Dried fruit and bean curd""
8,""Seafood"",""Seaweed and fish""
""1 of 1""
"));
        }

        private string NormalizeEOL(string input)
        {
            return Regex.Replace(input, @"\r\n|\n\r|\n|\r", "\r\n");
        }
    }
}