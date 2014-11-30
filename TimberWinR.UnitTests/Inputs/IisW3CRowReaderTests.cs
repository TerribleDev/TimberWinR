namespace TimberWinR.UnitTests.Inputs
{
    using System;
    using System.Collections.Generic;

    using Interop.MSUtil;

    using Moq;

    using NUnit.Framework;

    using TimberWinR.Inputs;
    using TimberWinR.Parser;

    [TestFixture]
    public class IisW3CRowReaderTests : TestBase
    {
        private IisW3CRowReader reader;

        public override void Setup()
        {
            base.Setup();
            var fields = new List<Field>
                             {
                                 new Field("date", "DateTime"),
                                 new Field("time", "DateTime"),
                                 new Field("uri")
                             };
            this.reader = new IisW3CRowReader(fields);

            var recordset = this.GetRecordsetMock();
            this.reader.ReadColumnMap(recordset.Object);
        }

        [Test]
        public void GivenValidRowAddsTimestampColumn()
        {
            var record = this.MockRepository.Create<ILogRecord>();
            record.Setup(x => x.getValue("date")).Returns(new DateTime(2014, 11, 30));
            record.Setup(x => x.getValue("time")).Returns(new DateTime(1, 1, 1, 18, 45, 37, 590));
            record.Setup(x => x.getValue("uri")).Returns("http://somedomain.com/someurl");

            var json = this.reader.ReadToJson(record.Object);

            Assert.AreEqual("2014-11-30T18:45:37.000Z", json["@timestamp"].ToString());
            Assert.AreEqual("http://somedomain.com/someurl", json["uri"].ToString());
        }

        private Mock<ILogRecordset> GetRecordsetMock()
        {
            var recordset = this.MockRepository.Create<ILogRecordset>();
            recordset.Setup(x => x.getColumnCount()).Returns(3);

            recordset.Setup(x => x.getColumnName(0)).Returns("date");
            recordset.Setup(x => x.getColumnName(1)).Returns("time");
            recordset.Setup(x => x.getColumnName(2)).Returns("uri");
            return recordset;
        }
    }
}
