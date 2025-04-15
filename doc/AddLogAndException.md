[Skip to
content](https://chatgpt.com/c/67fe5bf8-03f8-8002-bcc3-ece760bcb1a0#main)

ChatGPT

Share

![User](https://lh3.googleusercontent.com/a/ACg8ocIaQcASGS-
wsKIOL5fYaPUmiSoLnOCGzzd1Uqpn4gtm=s96-c)

##### You said:

give me a dotnet 8 , webapi application, I need global exception handle and
log feature.

###### ChatGPT said:

Here’s a minimal but solid starting point for a **.NET 8 Web API** application
with:

  1.  **Global Exception Handling** using middleware.

  2.  **Logging** using built-in `ILogger`.

* * *

### ✅ Step-by-Step Setup

#### 1\. Create the Project

    bash

    CopyEdit

    dotnet new webapi -n GlobalExceptionDemo
    cd GlobalExceptionDemo

* * *

#### 2\. Add Global Exception Middleware

Create a new file: `Middleware/ExceptionMiddleware.cs`

    csharp

    CopyEdit

    using System.Net;
    using System.Text.Json;

    namespace GlobalExceptionDemo.Middleware;

    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var response = _env.IsDevelopment()
                    ? new { status = 500, message = ex.Message, stackTrace = ex.StackTrace }
                    : new { status = 500, message = "Internal Server Error" };

                var json = JsonSerializer.Serialize(response);
                await context.Response.WriteAsync(json);
            }
        }
    }

* * *

#### 3\. Register the Middleware

Update `Program.cs`:

    csharp

    CopyEdit

    using GlobalExceptionDemo.Middleware;

    var builder = WebApplication.CreateBuilder(args);

    // Logging is already configured by default
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    // Add the middleware BEFORE other middlewares
    app.UseMiddleware<ExceptionMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseAuthorization();
    app.MapControllers();
    app.Run();

* * *

#### 4\. Example Controller

Create `Controllers/TestController.cs`:

    csharp

    CopyEdit

    using Microsoft.AspNetCore.Mvc;

    namespace GlobalExceptionDemo.Controllers;

    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet("exception")]
        public IActionResult ThrowException()
        {
            throw new Exception("This is a test exception");
        }

        [HttpGet("hello")]
        public IActionResult SayHello()
        {
            return Ok("Hello, world!");
        }
    }

* * *

### ✅ Run It

    bash

    CopyEdit

    dotnet run

  * Open Swagger UI at `https://localhost:<port>/swagger`

  * Test `/test/exception` to see the global handler in action.

* * *

Would you like me to add:

  * File or database logging (e.g., with **Serilog** )?

  * Custom exception classes (like `AppException`)?

  * ProblemDetails compliance for RFC 7807 standard?

Let me know how you want to expand this!

##### You said:

add Custom exception classes and give comment to explain why. like tech blog.

###### ChatGPT said:

Awesome — adding **custom exception classes** is a great way to make your code
**cleaner, more descriptive** , and easier to **log, test, and respond to with
appropriate HTTP codes**.

* * *

Here's the updated version of the solution, now including:

  * `AppException` base class

  * Specific custom exceptions: `NotFoundException`, `BadRequestException`

  * Enhanced middleware to respond with appropriate status codes

  * Inline comments that explain the _why_ — like a technical blog tutorial

* * *

### 🔧 Step 1: Add Custom Exception Classes

📁 **Create folder:** `Exceptions`

#### `Exceptions/AppException.cs`

    csharp

    CopyEdit

    namespace GlobalExceptionDemo.Exceptions;

    /// <summary>
    /// Base class for custom exceptions in the app.
    /// Helps us differentiate between "known" and "unknown" errors,
    /// so we can return proper HTTP codes instead of always 500.
    /// </summary>
    public abstract class AppException : Exception
    {
        public int StatusCode { get; }

        protected AppException(string message, int statusCode = 400) : base(message)
        {
            StatusCode = statusCode;
        }
    }

* * *

#### `Exceptions/NotFoundException.cs`

    csharp

    CopyEdit

    namespace GlobalExceptionDemo.Exceptions;

    /// <summary>
    /// Use this when a resource (e.g., user, file, order) is not found.
    /// Middleware will convert this into HTTP 404.
    /// </summary>
    public class NotFoundException : AppException
    {
        public NotFoundException(string message)
            : base(message, 404)
        {
        }
    }

* * *

#### `Exceptions/BadRequestException.cs`

    csharp

    CopyEdit

    namespace GlobalExceptionDemo.Exceptions;

    /// <summary>
    /// Use this for business rule violations or validation errors.
    /// This results in HTTP 400 (Bad Request).
    /// </summary>
    public class BadRequestException : AppException
    {
        public BadRequestException(string message)
            : base(message, 400)
        {
        }
    }

* * *

### 🧠 Why Use Custom Exceptions?

  * They separate _business errors_ from _programmer errors_

  * You can attach extra data (like `StatusCode`)

  * They make your `try/catch` cleaner

  * Middleware can _respond smartly_ based on type

* * *

### 🧩 Step 2: Update Middleware to Handle Custom Exceptions

📁 Modify `Middleware/ExceptionMiddleware.cs`

    csharp

    CopyEdit

    using System.Net;
    using System.Text.Json;
    using GlobalExceptionDemo.Exceptions;

    namespace GlobalExceptionDemo.Middleware;

    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context); // Pass to the next middleware/component
            }
            catch (AppException ex) // Handle known custom exceptions
            {
                _logger.LogWarning(ex, "Handled application error");
                await WriteErrorResponse(context, ex.StatusCode, ex.Message);
            }
            catch (Exception ex) // Handle everything else (unexpected errors)
            {
                _logger.LogError(ex, "Unhandled exception");
                var msg = _env.IsDevelopment() ? ex.Message : "Internal Server Error";
                await WriteErrorResponse(context, 500, msg);
            }
        }

        private async Task WriteErrorResponse(HttpContext context, int statusCode, string message)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var response = new
            {
                status = statusCode,
                error = message
            };

            var json = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(json);
        }
    }

* * *

### 🔍 Step 3: Example Usage in Controller

📁 Update `Controllers/TestController.cs`

    csharp

    CopyEdit

    using Microsoft.AspNetCore.Mvc;
    using GlobalExceptionDemo.Exceptions;

    namespace GlobalExceptionDemo.Controllers;

    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet("exception")]
        public IActionResult ThrowGeneralException()
        {
            throw new Exception("This is a general exception.");
        }

        [HttpGet("notfound")]
        public IActionResult ThrowNotFound()
        {
            throw new NotFoundException("User not found.");
        }

        [HttpGet("badrequest")]
        public IActionResult ThrowBadRequest()
        {
            throw new BadRequestException("Invalid input format.");
        }

        [HttpGet("hello")]
        public IActionResult SayHello()
        {
            return Ok("Hello, world!");
        }
    }

* * *

### ✅ Final Thoughts (like a blog conclusion)

By introducing a structured exception hierarchy:

  * Your API becomes more **predictable** — clients get 400/404 instead of generic 500s.

  *  **Logs are cleaner** — you can filter by severity.

  * Your **code remains clean** — no cluttered try/catch all over.

  * Easy to extend later with `UnauthorizedException`, `ConflictException`, etc.

* * *

Let me know if you want to integrate **ProblemDetails** , **FluentValidation**
, or **Serilog** for better developer/consumer experience!

Search

Reason

ChatGPT can make mistakes. Check important info.

?
