using NUnit.Framework;
using RestSharp;
using FoodyApiTests.Models;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using RestSharp.Authenticators;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Runtime.CompilerServices;


namespace FoodyApiTests;

[TestFixture]

public class FoodyTests
{
    private RestClient _client;
    private static string? createdFoodId;
    private const string baseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:86";

    [OneTimeSetUp]
    public void Setup()
    {
        string token = GetJwtToken("ivoivanov", "ivo123456789");

        var options = new RestClientOptions(baseUrl)
        {
            Authenticator = new JwtAuthenticator(token)
        };

        _client = new RestClient(options);


    }

    private string GetJwtToken(string username, string password)
    {
        var loginClient = new RestClient(baseUrl);
        var request = new RestRequest("api/User/Authentication", Method.Post);
        request.AddJsonBody(new { username, password });
        var response = loginClient.Execute(request);
        var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
        return json.GetProperty("accessToken").GetString() ?? string.Empty;
    }



    //Tests
    [Test, Order(1)]
    public void CreateFood_ShouldReturnCreated()
    {
        var food = new
        {
            name = "New Food",
            description = "Delicious new food item",
            url = ""  // optional
        };

        var request = new RestRequest("/api/Food/Create", Method.Post);
        request.AddJsonBody(food);

        var response = _client.Execute<ApiResponseDTO>(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

        createdFoodId = json.GetProperty("foodId").GetString() ?? string.Empty;

        Assert.That(createdFoodId, Is.Not.Null.And.Not.Empty, "Food ID should not be null or empty");
    }

    [Test, Order(2)]
    public void EditFoodTitle_ShouldReturnOK()
    {
        // Skip this test if createdFoodId is not set
        if (string.IsNullOrEmpty(createdFoodId))
        {
            Assert.Inconclusive("Food ID not available. CreateFood test may have failed.");
            return;
        }

        var changes = new[]
        {
            new { path = "/name", op = "replace", value = "Updated food name" }
        };

        var request = new RestRequest($"/api/Food/Edit/{createdFoodId}", Method.Patch);
        request.AddJsonBody(changes);

        var response = _client.Execute<ApiResponseDTO>(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content, Does.Contain("Successfully edited"));
    }

    [Test, Order(3)]

    public void GetAllFoods_ShouldReturnList()
    {
        var request = new RestRequest("/api/Food/All", Method.Get);

        var response = _client.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var foods = JsonSerializer.Deserialize<List<object>>(response.Content);

        Assert.That(foods, Is.Not.Empty);
    }

    [Test, Order(4)]

    public void DeleteFood_ShouldReturnOK()
    {
        // Skip this test if createdFoodId is not set
        if (string.IsNullOrEmpty(createdFoodId))
        {
            Assert.Inconclusive("Food ID not available. CreateFood test may have failed.");
            return;
        }

        var request = new RestRequest($"/api/Food/Delete/{createdFoodId}", Method.Delete);

        var response = _client.Execute<ApiResponseDTO>(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        // Confirm that the response message is "Deleted successfully!"
        if (response.Data != null)
        {
            Assert.That(response.Data.Msg, Is.EqualTo("Deleted successfully!"));
        }
        else
        {
            Assert.Fail("Response data is null");
        }
    }

    [Test, Order(5)]

    public void CreateFoodWithoutID_ShouldReturnBadRequest()
    {
        var food = new
        {
            name = "",
            description = "",
            url = ""  // optional
        };

        var request = new RestRequest("/api/Food/Create", Method.Post);
        request.AddJsonBody(food);
        var response = _client.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
    [Test, Order(6)]

    public void FoodEditNonExisting_ShouldReturnNotFound()
    {
        string fakeID = "1234";
        var changes = new[]
        {
            new { path = "/name", op = "replace", value = "NonExistentFood" }
        };

        var request = new RestRequest($"/api/Food/Edit/{fakeID}", Method.Patch);
        request.AddJsonBody(changes);

        var response = _client.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(response.Content, Does.Contain("No food revues..."));
    }
    [Test, Order(7)]

    public void DeleteNonExistingFood_ShouldReturnBadRequest()
    {
        string fakeID = "1234";

        var request = new RestRequest($"/api/Food/Delete/{fakeID}", Method.Delete);
        var response = _client.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(response.Content, Does.Contain("Unable to delete this food revue!"));
    }



    [OneTimeTearDown]

    public void Cleanup()
    {
        _client?.Dispose();

    }
}