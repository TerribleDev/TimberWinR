using TimberWinR.Outputs;

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

        [Test]
        public void Given_no_ssl_then_validate_does_not_throw()
        {
            parser.Ssl = false;
            Assert.That(() => parser.Validate(), Throws.Nothing);
        }

        [Test]
        public void Given_ssl_and_no_username_then_validate_throws()
        {
            parser.Ssl = true;
            parser.Password = "pass";

            Assert.That(() => parser.Validate(), Throws.Exception.InstanceOf<ElasticsearchOutputParameters.ElasticsearchBasicAuthException>());
        }

        [Test]
        public void Given_ssl_and_no_password_then_validate_throws()
        {
            parser.Ssl = true;
            parser.Username = "user";

            Assert.That(() => parser.Validate(), Throws.Exception.InstanceOf<ElasticsearchOutputParameters.ElasticsearchBasicAuthException>());
        }

        [Test]
        public void Given_ssl_and_username_and_password_then_validate_does_not_throw()
        {
            parser.Ssl = true;
            parser.Username = "user";
            parser.Password = "pass";

            Assert.That(() => parser.Validate(), Throws.Nothing);
        }

        [Test]
        [TestCase("host", 1234, false, null, null, "http://host:1234/")]
        [TestCase("host", 1234, true, "user", "pass", "https://user:pass@host:1234/")]
        [TestCase("host", 1234, true, "user:", "pass@", "https://user%3A:pass%40@host:1234/")]
        public void ComposeUri_Matches_Expected(string host, int port, bool ssl, string username, string password, string expectedUri)
        {
            var uri = ElasticsearchOutput.ComposeUri(host, port, ssl, username, password);

            Assert.That(uri.ToString(), Is.EqualTo(expectedUri));
        }
    }
}
