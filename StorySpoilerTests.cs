using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using StorySpoiler.Models;
using System.Net;
using System.Text.Json;


namespace StorySpoilerSystem
{
    public class StoryTests
    {
        private RestClient client;
        private static string createdStoryId;
        private const string baseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";

        [SetUp]
        public void Setup()
        {
            string token = GetJwtToken("qatestertedd", "123456");

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            var loginClient = new RestClient(baseUrl);

            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { username, password });

            var response = loginClient.Execute(request);

            if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
            {
                throw new InvalidOperationException("Failed to retrieve JWT token.");
            }

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            return json.GetProperty("accessToken").GetString() ?? string.Empty;
        }

        [TearDown]
        public void TearDown()
        {
            client?.Dispose();
        }

        [Order(1)]
        [Test]
        public void CreateStory_ShouldReturnCreated()
        {
            var storyInfo = new
            {
                Title = "New Story",
                Description = "My first story",
                Url = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(storyInfo);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created),
                $"Expected Created, but got {response.StatusCode}. Response: {response.Content}");

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            createdStoryId = json.GetProperty("storyId").GetString() ?? string.Empty;

            Assert.That(createdStoryId, Is.Not.Null.And.Not.Empty);
        }

        [Order(2)]
        [Test]
        public void EditStoryTitle_ShouldReturnOK()
        {
            var updatedStory = new
            {
                Title = "Updated story title",
                Description = "My first story",
                Url = ""
            };

            var request = new RestRequest($"/api/Story/Edit/{createdStoryId}", Method.Put);
            request.AddJsonBody(updatedStory);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"Expected OK, but got {response.StatusCode}. Response: {response.Content}");

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            string message = json.GetProperty("msg").GetString() ?? string.Empty;

            Assert.AreEqual("Successfully edited", message);
        }

        [Order(3)]
        [Test]
        public void GetAllStories_ShouldReturnAllItems()
        {
            var request = new RestRequest("/api/Story/All", Method.Get);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var stories = JsonSerializer.Deserialize<List<object>>(response.Content);
            Assert.That(stories, Is.Not.Empty);
        }

        [Order(4)]
        [Test]
        public void DeleteStory_ShouldReturnOk()
        {
            var request = new RestRequest($"/api/Story/Delete/{createdStoryId}", Method.Delete);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var data = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(response.Content, Does.Contain("Deleted successfully!"));
        }

        [Order(5)]
        [Test]
        public void CreateStory_MissingFields_ShouldReturnBadRequest()
        {
            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(new StoryDTO { Title = "", Description = "" });

            var response = client.Execute(request);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Order(6)]
        [Test]
        public void EditNonExistingStory_ShouldReturnNotFound()
        {
            
            var nonExistingId = Guid.NewGuid();

            var request = new RestRequest($"/api/Story/Edit/{nonExistingId}", Method.Put);

            var body = new
            {
                Title = "EditNonExistingStory",
                Description = "Trying to edit a story that doesn't exist",
                Url = ""
            };
            request.AddJsonBody(body);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
                $"Expected NotFound (404), but got {response.StatusCode}. Response: {response.Content}");

            if (!string.IsNullOrWhiteSpace(response.Content))
            {
                var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
                string message = json.GetProperty("msg").GetString() ?? string.Empty;

                Assert.AreEqual("No spoilers...", message);
            }

        }
        [Order(7)]
        [Test]
        public void DeleteNonExistingStory_ShouldReturnBadRequest()
        {

            var nonExistingId = Guid.NewGuid();
            var request = new RestRequest($"/api/Story/Delete/{nonExistingId}", Method.Delete);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                $"Expected BadRequest(400), but got {response.StatusCode}. Response: {response.Content}");

            if (!string.IsNullOrWhiteSpace(response.Content))
            {
                var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
                string message = json.GetProperty("msg").GetString() ?? string.Empty;

                Assert.AreEqual("Unable to delete this story spoiler!", message);


            }


        }
    }
}