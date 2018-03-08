using GStore.Core.Data;
using GStore.Core.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using System.IO;
using Microsoft.Extensions.Logging;

namespace GStore.Test
{
    [TestClass]
    public class GeoRepositoryTest
    {
        DataContext _context;

        [TestInitialize]
        public void Initialize()
        {
            var configurationBuilder = new ConfigurationBuilder()
                                             .SetBasePath( Directory.GetCurrentDirectory() )
                                             .AddJsonFile( "appsettings.json" );

            var configuration = configurationBuilder.Build();

            var logger = new LoggerFactory().CreateLogger<DataContext>();

            _context = new DataContext( configuration, logger );
        }

        [TestMethod]
        public void GetByLocation_shouldreturn_notemptylist()
        {
            var repository = new GeoRepository<GeoData>( _context );

            var radius = 50 / 3963.2;

            var result = repository.GetByLocation( 14.271879, 40.852815, radius );

            Assert.IsNotNull( result.Count > 0, "no data found" );
        }
    }
}
