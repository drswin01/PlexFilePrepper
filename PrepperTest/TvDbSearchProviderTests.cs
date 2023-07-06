namespace PrepperTests
{
    public class TvDbSearchProviderTests
    {
        [Test]
        public void BadLoginInfoShouldThrowException()
        {
            try
            {
                IReferenceProvider searchProvider = TheTvDbProvider.GetProvider("1234", "dude");
                Assert.Fail("No exception thrown with bad login info.");
            }
            catch (Exception)
            { }
        }

        //TODO: Update with mocks
        //[Test]
        //public void SeriesSearchShouldPopulateLatestResults()
        //{
        //    IReferenceProvider searchProvider = TheTvDbProvider.GetProvider("1234", "dude");
        //    searchProvider.SetMediaType("Series");
        //    searchProvider.PerformTitleSearch("Star Trek");
        //    Assert.That(searchProvider.GetSearchResults(), Is.Not.Null);
        //}

        //TODO: Update with mocks
        //[Test]
        //public void GetEpisodesForTitlePagedShouldReturnResults()
        //{
        //    IReferenceProvider searchProvider = TheTvDbProvider.GetProvider("1234", "dude");
        //    searchProvider.SetMediaType("Series");
        //    searchProvider.PerformTitleSearch("Star Trek: Deep Space Nine");
        //    Assert.That(searchProvider.GetEpisodesForTitle("Star Trek: Deep Space Nine"), Is.Not.Null);
        //}
    }
}
