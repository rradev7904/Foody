using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;

namespace Foody
{
    [TestFixture]
    public class FoodyTests
    {
        private RestClient client;
        private static string createdFoodId;
        private const string baseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:86";

        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("rradev111", "rradev111");
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
            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            return json.GetProperty("accessToken").GetString() ?? string.Empty;
        }

        [Test, Order(1)]
        public void CreateFood_ShouldReturnCreated()
        {
            var food = new
            {
                name = "Test Food",
                description = "This is a test food item.",
                url = ""
            };
            var request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(food);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), "Expected status code 201 Created.");
            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            createdFoodId = json.GetProperty("foodId").GetString() ?? string.Empty;
            Assert.That(createdFoodId, Is.Not.Null.And.Not.Empty, "Food ID should not be null or empty");
        }

        [Test, Order(2)]
        public void EditFoodTitle_ShouldReturnOk()
        {
            var changes = new[]
            {
                new { path = "/name", op = "replace", value = "Updated food name" }
            };
            var request = new RestRequest($"/api/Food/Edit/{createdFoodId}", Method.Patch);
            request.AddJsonBody(changes);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(response.Content, Does.Contain("Successfully edited"), "Expected message for successfully edited food.");

        }

        [Test, Order(3)]
        public void GetAllFoods_ShouldReturnList()
        {
            var request = new RestRequest("/api/Food/All", Method.Get);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            var foods = JsonSerializer.Deserialize<List<object>>(response.Content);
            Assert.That(foods, Is.Not.Empty, "Foods list should not be null.");
        }

        [Test, Order(4)]
        public void DeleteFood_ShouldReturnNoContent()
        {
            var request = new RestRequest($"/api/Food/Delete/{createdFoodId}", Method.Delete);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 No Content.");
            Assert.That(response.Content, Does.Contain("Deleted successfully!"), "Expected message for successfully deleted food.");
        }

        [Test, Order(5)]
        public void CreateFoodWithoutRequiredFields_ShouldReturnBadRequest()
        {
            var food = new
            {
                name = "",
                description = "",
                url = ""
            };
            var request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(food);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
        }

        [Test, Order(6)]
        public void EditNonExistingFood_ShouldReturnNotFound()
        {
            string invalidFoodId = "invalid-id";
            var changes = new[]
            {
                new { path = "/name", op = "replace", value = "Updated food name" }
            };
            var request = new RestRequest($"/api/Food/Edit/{invalidFoodId}", Method.Patch);
            request.AddJsonBody(changes);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "Expected status code 404 Not Found.");
            Assert.That(response.Content, Does.Contain("No food revues.."), "Expected error message for non-existing food.");
        }

        [Test, Order(7)]
        public void DeleteNonExistingFood_ShouldReturnNotFound()
        {
            string invalidFoodId = "invalid-id";
            var request = new RestRequest($"/api/Food/Delete/{invalidFoodId}", Method.Delete);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
            Assert.That(response.Content, Does.Contain("Unable to delete this food revue!"), "Expected error message for non-existing food.");
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            client.Dispose();
        }
    }
}