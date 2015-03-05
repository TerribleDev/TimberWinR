namespace TimberWinR.UnitTests.Parser
{
    using System;

    using Newtonsoft.Json.Linq;

    using NUnit.Framework;

    using TimberWinR.Parser;

    public class ElasticsearchOutputTests
    {
        private ElasticsearchOutputParameters parser;

        [SetUp]
        public void Setup()
        {
            this.parser = new ElasticsearchOutputParameters();
        }

        [Test]
        public void Given_no_index_returns_default_index_name()
        {
            this.parser.Index = "someindex";
            var json = new JObject();

            var result = this.parser.GetIndexName(json);

            Assert.AreEqual("someindex", result);
        }

        [Test]
        public void Given_index_with_date_format_and_timestamp_returns_name_by_timestamp()
        {
            this.parser.Index = "someindex-%{yyyy.MM.dd}";
            var json = new JObject();
            json.Add(new JProperty("@timestamp", "2011-11-30T18:45:32.450Z"));

            var result = this.parser.GetIndexName(json);

            Assert.AreEqual("someindex-2011.11.30", result);
        }

        [Test]
        public void Given_index_with_date_format_and_no_timestamp_returns_name_by_current_date()
        {
            this.parser.Index = "someindex-%{yyyy.MM.dd}";
            var json = new JObject();

            var result = this.parser.GetIndexName(json);

            Assert.AreEqual("someindex-" + DateTime.UtcNow.ToString("yyyy.MM.dd"), result);
        }
    }
}
