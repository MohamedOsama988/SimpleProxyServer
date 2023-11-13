
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.MapGet("/proxy", async (HttpContext context) =>
{
    var url = context.Request.Query["url"];
    if (string.IsNullOrEmpty(url))
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("url query parameter is required");
        return;
    }
    var headers = context.Request.Headers;
    var requestBody = await new System.IO.StreamReader(context.Request.Body).ReadToEndAsync();
    var response = await ProxyRequest(url, headers, requestBody);

    foreach (var header in response.Headers)
    {
        context.Response.Headers.Add(header.Key, header.Value.ToArray());
    }

    context.Response.StatusCode = (int)response.StatusCode;
    await response.Content.CopyToAsync(context.Response.Body);
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

async Task<HttpResponseMessage> ProxyRequest(string url, IHeaderDictionary headers, string body)
{
    using (var client = new HttpClient())
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Copy request headers
        foreach (var header in headers)
        {
            request.Headers.Add(header.Key, header.Value.ToArray());
        }

        // Copy request body
        request.Content = new StringContent(body);

        // Send the request to the target API
        return await client.SendAsync(request);
    }
}