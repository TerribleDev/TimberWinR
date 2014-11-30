namespace TimberWinR.UnitTests
{
    using Moq;

    using NUnit.Framework;

    public class TestBase
    {
        public MockRepository MockRepository { get; private set; }

        [SetUp]
        public virtual void Setup()
        {
            this.MockRepository = new MockRepository(MockBehavior.Default);
        }

        [TearDown]
        public virtual void TearDown()
        {
            this.MockRepository.VerifyAll();
        }
    }
}
