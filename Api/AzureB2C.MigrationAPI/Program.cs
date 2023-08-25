using System.Net;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

var clientId = app.Configuration["ClientId"];
var tenant = app.Configuration["Tenant"];
var domain = app.Configuration["Domain"];

app.MapPost("/SignUpAsync", async Task<Results<Ok<OutputClaimsModel>, Conflict<B2CResponseContent>, BadRequest<string>>> (InputClaimsModel input, HttpClient httpClient) =>
{

    if(input == null || string.IsNullOrWhiteSpace(input.signInName) || string.IsNullOrWhiteSpace(input.password))
    {
        return TypedResults.BadRequest("Invalid Request");
    }
        
    var outputClaims = new OutputClaimsModel();

    var username = $"{input.signInName}@{domain}";
    var password = System.Web.HttpUtility.UrlEncode(input.password);

    var tokenEndpoint = $"https://login.windows.net/{tenant}/oauth2/token";
    var accept = "application/json";
    httpClient.DefaultRequestHeaders.Add("Accept", accept);

    var postBody = $"resource=https%3A%2F%2Fgraph.windows.net%2F&client_id={clientId}&username={username}&password={password}&scope=openid&grant_type=password";

    using (var response = await httpClient.PostAsync(tokenEndpoint, new StringContent(postBody, Encoding.UTF8, "application/x-www-form-urlencoded")))
    {
        //if the creds auth'd - create the user with graph api into b2c
        if (response.IsSuccessStatusCode)
        {
            //Run token validation

            outputClaims.tokenSuccess = true;
            outputClaims.migrationRequired = false;
            return TypedResults.Ok(outputClaims);
        }

        var responseBody = await response.Content.ReadAsStringAsync();

        Console.WriteLine(responseBody);

        return TypedResults.Conflict(new B2CResponseContent("Migrater API - Incorrect password", HttpStatusCode.Conflict));
    }
    
});

app.Run();

public class B2CResponseContent
{
    public string version { get; set; }
    public int status { get; set; }
    public string userMessage { get; set; }

    public B2CResponseContent(string message, HttpStatusCode status)
    {
        this.userMessage = message;
        this.status = (int)status;
        this.version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }
}

public class InputClaimsModel
{
    public string signInName { get; set; }
    public string password { get; set; }
}

public class OutputClaimsModel
{
    public bool tokenSuccess { get; set; }
    public bool migrationRequired { get; set; }
}



