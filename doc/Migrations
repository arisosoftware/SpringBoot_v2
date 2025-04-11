Sure! Below is a full solution for a .NET Core WebAPI that acts as a forward proxy for WCF calls, capturing requests and responses, and storing them in an SQLite database.

### Complete Solution Overview

#### 1. Project Structure
```
WcfForwardProxy/
│
├── Controllers/
│   └── ProxyController.cs
│
├── Data/
│   └── SoapLogContext.cs
│
├── Models/
│   └── SoapLog.cs
│
├── Migrations/
│   └── (auto-generated files)
│
├── appsettings.json
│
├── Program.cs
│
└── WcfForwardProxy.csproj
```

### Step-by-Step Implementation

#### 1. Create a New .NET Core WebAPI Project
Run the following command to create a new project:

```bash
dotnet new webapi -n WcfForwardProxy
cd WcfForwardProxy
```

#### 2. Install Necessary NuGet Packages
Add the required packages for Entity Framework Core with SQLite and WCF client:

```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package System.ServiceModel.Http
```

#### 3. Create the Models
Create the `SoapLog` model to represent the log entry.

##### **Models/SoapLog.cs**
```csharp
using System;

namespace WcfForwardProxy.Models
{
    public class SoapLog
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public string SoapAction { get; set; }
        public string RequestXml { get; set; }
        public string ResponseXml { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
```

#### 4. Set Up the Database Context
Create a database context to handle the SQLite database operations.

##### **Data/SoapLogContext.cs**
```csharp
using Microsoft.EntityFrameworkCore;
using WcfForwardProxy.Models;

namespace WcfForwardProxy.Data
{
    public class SoapLogContext : DbContext
    {
        public SoapLogContext(DbContextOptions<SoapLogContext> options) : base(options) { }

        public DbSet<SoapLog> SoapLogs { get; set; }
    }
}
```

#### 5. Configure the Database in Program.cs
Update the `Program.cs` to configure the SQLite database and add services.

##### **Program.cs**
```csharp
using Microsoft.EntityFrameworkCore;
using WcfForwardProxy.Data;

var builder = WebApplication.CreateBuilder(args);

// Configure SQLite database
builder.Services.AddDbContext<SoapLogContext>(options =>
    options.UseSqlite("Data Source=logs.db"));

builder.Services.AddHttpClient(); // For forwarding requests

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();
app.Run();
```

#### 6. Create the Proxy Controller
Create a controller to handle incoming WCF requests and forward them to the backend service.

##### **Controllers/ProxyController.cs**
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using WcfForwardProxy.Data;
using WcfForwardProxy.Models;

namespace WcfForwardProxy.Controllers
{
    [ApiController]
    [Route("{**catchAll}")]
    public class ProxyController : ControllerBase
    {
        private readonly SoapLogContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public ProxyController(SoapLogContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost]
        public async Task<IActionResult> Forward()
        {
            // Read the request body
            var body = await new StreamReader(Request.Body).ReadToEndAsync();
            var soapAction = Request.Headers["SOAPAction"].ToString();
            var originalUrl = $"https://backend-wcf-server.com/{Request.Path}"; // Change to your WCF server URL

            // Log request to SQLite
            var logEntry = new SoapLog
            {
                Path = Request.Path,
                SoapAction = soapAction,
                RequestXml = body,
                Timestamp = DateTime.UtcNow
            };
            await _context.SoapLogs.AddAsync(logEntry);
            await _context.SaveChangesAsync();

            // Create the HTTP client to forward the request
            var client = _httpClientFactory.CreateClient();
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, originalUrl)
            {
                Content = new StringContent(body, Encoding.UTF8, "text/xml")
            };
            requestMessage.Headers.Add("SOAPAction", soapAction);

            // Forward the request
            var response = await client.SendAsync(requestMessage);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Log response to SQLite
            logEntry.ResponseXml = responseBody;
            await _context.SaveChangesAsync();

            return Content(responseBody, "text/xml");
        }
    }
}
```

#### 7. Create Database Migrations
Run the following commands to create the initial migration and update the database:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 8. Testing the Proxy
You can now run your WebAPI and point your existing WCF clients to this proxy URL instead of the original WCF server. For example, if your proxy is running locally:

```
http://localhost:5000/MyService.svc
```

### 9. Log Inspection
You can inspect the logs stored in the `logs.db` SQLite database to see the recorded requests and responses.

---

## Additional Considerations
- **Error Handling**: You might want to implement try-catch blocks to handle exceptions and return appropriate error responses.
- **Timeout Settings**: Customize timeouts for the forwarded requests as needed.
- **Logging to a Text File**: If you want to log requests/responses to a text file as well, you can use `StreamWriter` to append entries.

### Conclusion
This complete solution provides a working .NET Core WebAPI that acts as a forward proxy for WCF services. You can extend the functionality as needed to meet your specific requirements.

If you have any questions or need further modifications, feel free to ask!
