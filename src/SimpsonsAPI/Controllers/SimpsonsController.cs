using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.Documents;
using Microsoft.Extensions.Configuration;
using System.Net;
using SimpsonsAPI.Model;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace SimpsonsAPI.Controllers
{
    [Route("api/[controller]")]
    public class SimpsonsController : Controller
    {
        #region Properties
        private static DocumentClient _client;
        private string _enpointUri;
        private string _key;
        private string _databaseId;
        private string _collectionId;
        public IConfigurationRoot Configuration { get; set; }
        #endregion

        public SimpsonsController()
        {
            BuildConfiguration();
            _enpointUri = Configuration.Get<string>("AzureSettings:endpointUri");
            _key = Configuration.Get<string>("AzureSettings:primaryKey");
            _databaseId = Configuration.Get<string>("AzureSettings:database");
            _collectionId = Configuration.Get<string>("AzureSettings:collection");
        }

        [HttpGet]
        public async Task<IEnumerable<CastMember>> Get()
        {
            List<CastMember> members = new List<CastMember>();

            using (_client = new DocumentClient(new Uri(_enpointUri), _key))
            {
                //Get the database
                Database db = await GetDatabase(_databaseId);

                //Get the collection
                DocumentCollection collection = GetCollection(db, _collectionId);

                //Get list of documents
                members = (from f in _client.CreateDocumentQuery<CastMember>(collection.SelfLink)
                           select f).ToList();

            }

            return members;
        }

        [HttpGet("{character}")]
        public async Task<CastMember> Get(string character)
        {
            CastMember member = new CastMember();

            using (_client = new DocumentClient(new Uri(_enpointUri), _key))
            {
                //Get the database
                Database db = await GetDatabase(_databaseId);

                //Get the collection
                DocumentCollection collection = GetCollection(db, _collectionId);

                //Find CastMember document by character name
                member = _client.CreateDocumentQuery<CastMember>(collection.SelfLink).ToList()
                    .Where(d => d.Characters.Contains(character)).SingleOrDefault();

            }

            return member;
        }

        [HttpPost]
        public async Task<string> Post([FromBody]CastMember newMember)
        {
            var status = HttpStatusCode.NotAcceptable;
            var message = "Cast member not created";

            using (_client = new DocumentClient(new Uri(_enpointUri), _key))
            {
                //Get the database
                Database db = await GetDatabase(_databaseId);

                //Get the collection
                DocumentCollection collection = GetCollection(db, _collectionId);

                //Create new document for member
                status = await CreateDocument(collection.SelfLink, newMember);

            }

            return status == HttpStatusCode.Created ? "Cast member created" : message;
        }

        [HttpDelete("{id}")]
        public async Task<string> Delete(string id)
        {
            var status = HttpStatusCode.BadRequest;
            var message = "Cast member not deleted";

            using (_client = new DocumentClient(new Uri(_enpointUri), _key))
            {
                //Get the database
                Database db = await GetDatabase(_databaseId);

                //Get the collection
                DocumentCollection collection = GetCollection(db, _collectionId);

                //Get the document from collection
                Document doc = _client.CreateDocumentQuery(collection.SelfLink,
                    "SELECT * FROM CastMembers c WHERE c.id = '" + id + "'").ToList().SingleOrDefault();

                //Create new document for member
                if (doc != null)
                {
                    var response = await _client.DeleteDocumentAsync(doc.SelfLink);
                    status = response.StatusCode;
                }
            }

            return status == HttpStatusCode.NoContent ? "Cast member deleted" : message;
        }

        #region Private Methods
        private async Task<Database> GetDatabase(string id)
        {
            Database database = _client.CreateDatabaseQuery().Where(c => c.Id == id).ToArray().FirstOrDefault();

            if (database == null)
            {
                database = await _client.CreateDatabaseAsync(new Database { Id = id });
            }

            return database;
        }

        private DocumentCollection GetCollection(Database db, string collection)
        {
            return _client.CreateDocumentCollectionQuery(UriFactory.CreateDatabaseUri(db.Id))
                            .Where(c => c.Id == collection)
                            .ToArray()
                            .SingleOrDefault();
        }

        private async Task<HttpStatusCode> CreateDocument(string selfLink, CastMember doc)
        {
            var response = await _client.CreateDocumentAsync(selfLink, doc);
            return response.StatusCode;
        }

        private void BuildConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }
        #endregion
    }
}