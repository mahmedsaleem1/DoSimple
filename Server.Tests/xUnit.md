# xUnit Testing Guide for DoSimple Project

## Table of Contents
1. [What is xUnit?](#what-is-xunit)
2. [Why xUnit?](#why-xunit)
3. [Key Concepts](#key-concepts)
4. [Project Test Structure](#project-test-structure)
5. [How Tests Work](#how-tests-work)
6. [Running Tests](#running-tests)
7. [Understanding Test Coverage](#understanding-test-coverage)
8. [Best Practices](#best-practices)

---

## What is xUnit?

**xUnit** is a free, open-source, community-focused unit testing framework for .NET applications. It was created by the original inventor of NUnit and is designed to be simple, extensible, and follow modern testing practices.

### Key Features:
- **Modern Design**: Built from the ground up for .NET Core and .NET 5+
- **Parallel Test Execution**: Tests run in parallel by default for faster execution
- **Extensible**: Easy to extend with custom attributes and assertions
- **Clean Syntax**: Uses standard C# features instead of attributes for setup/teardown
- **Strong Community**: Widely adopted in the .NET ecosystem

---

## Why xUnit?

| Feature | xUnit | NUnit | MSTest |
|---------|-------|-------|--------|
| Modern .NET Support | ✅ Excellent | ✅ Good | ✅ Good |
| Parallel Execution | ✅ Default | ⚠️ Manual | ⚠️ Manual |
| Constructor Injection | ✅ Yes | ❌ No | ❌ No |
| Clean Test Isolation | ✅ Yes | ⚠️ Shared State | ⚠️ Shared State |
| Open Source | ✅ Yes | ✅ Yes | ❌ Microsoft |

---

## Key Concepts

### 1. Test Class
A regular C# class containing test methods. In xUnit, the class constructor acts as the setup and `IDisposable.Dispose()` acts as cleanup.

```csharp
public class MyServiceTests : IDisposable
{
    private readonly MyService _service;

    // Constructor = Setup (runs before each test)
    public MyServiceTests()
    {
        _service = new MyService();
    }

    // Dispose = Teardown (runs after each test)
    public void Dispose()
    {
        _service.Cleanup();
    }

    [Fact]
    public void MyTest()
    {
        // Test code here
    }
}
```

### 2. [Fact] Attribute
Marks a method as a test with no parameters. Use for single test cases.

```csharp
[Fact]
public void Add_TwoNumbers_ReturnsSum()
{
    // Arrange
    var calculator = new Calculator();
    
    // Act
    var result = calculator.Add(2, 3);
    
    // Assert
    Assert.Equal(5, result);
}
```

### 3. [Theory] Attribute
Marks a method as a parameterized test that runs multiple times with different data.

```csharp
[Theory]
[InlineData(1, 2, 3)]
[InlineData(0, 0, 0)]
[InlineData(-1, 1, 0)]
public void Add_WithVariousInputs_ReturnsCorrectSum(int a, int b, int expected)
{
    var calculator = new Calculator();
    var result = calculator.Add(a, b);
    Assert.Equal(expected, result);
}
```

### 4. Assertions
xUnit provides various assertion methods to verify test outcomes:

```csharp
// Equality
Assert.Equal(expected, actual);
Assert.NotEqual(unexpected, actual);

// Boolean
Assert.True(condition);
Assert.False(condition);

// Null checks
Assert.Null(obj);
Assert.NotNull(obj);

// Collections
Assert.Empty(collection);
Assert.Contains(item, collection);
Assert.All(collection, item => Assert.True(item > 0));

// Exceptions
Assert.Throws<ArgumentException>(() => method());
await Assert.ThrowsAsync<InvalidOperationException>(async () => await asyncMethod());

// Type checking
Assert.IsType<ExpectedType>(obj);
```

### 5. Mocking with Moq
We use the **Moq** library to create mock objects for dependencies:

```csharp
// Create a mock
var mockService = new Mock<IEmailService>();

// Setup behavior
mockService.Setup(x => x.SendEmailAsync(It.IsAny<string>()))
    .ReturnsAsync(true);

// Use the mock
var authService = new AuthService(mockService.Object);

// Verify calls
mockService.Verify(x => x.SendEmailAsync("test@example.com"), Times.Once);
```

---

## Project Test Structure

```
Server.Tests/
├── Server.Tests.csproj          # Test project configuration
├── Helpers/                      # Test utilities and factories
│   ├── TestDbContextFactory.cs   # Creates in-memory databases
│   ├── MockServiceFactory.cs     # Creates mock services
│   └── TestConfigurationFactory.cs # Creates test configurations
├── Services/                     # Service layer tests
│   ├── AuthServiceTests.cs       # Authentication tests
│   ├── TaskServiceTests.cs       # Task management tests
│   └── UserServiceTests.cs       # User management tests
├── Controllers/                  # Controller/Endpoint tests
│   ├── AuthControllerTests.cs    # Auth API endpoint tests
│   ├── TaskControllerTests.cs    # Task API endpoint tests
│   └── UserControllerTests.cs    # User API endpoint tests
├── Utilities/                    # Utility class tests
│   ├── PasswordHasherTests.cs    # Password hashing tests
│   └── JwtTokenGeneratorTests.cs # JWT token tests
└── Data/                         # Data access layer tests
    └── AppDbContextTests.cs      # Database context tests
```

---

## How Tests Work

### Service Tests (AuthServiceTests, TaskServiceTests, UserServiceTests)

These tests verify business logic without making real database or external API calls:

```
┌─────────────────────────────────────────────────────────────┐
│                     Service Test Flow                        │
├─────────────────────────────────────────────────────────────┤
│  1. Create In-Memory Database (TestDbContextFactory)        │
│           ↓                                                  │
│  2. Create Mock External Services (Moq)                     │
│      - IEmailService (mock)                                 │
│      - ICloudinaryService (mock)                            │
│           ↓                                                  │
│  3. Instantiate Real Service with Dependencies              │
│           ↓                                                  │
│  4. Execute Test (Call service method)                      │
│           ↓                                                  │
│  5. Assert Results (Verify behavior)                        │
│           ↓                                                  │
│  6. Cleanup (Dispose database)                              │
└─────────────────────────────────────────────────────────────┘
```

**Example from AuthServiceTests:**

```csharp
[Fact]
public async Task RegisterAsync_WithValidRequest_ReturnsRegisterResponse()
{
    // 1. Arrange - Setup is done in constructor
    var request = new RegisterRequest
    {
        Name = "New User",
        Email = "newuser@example.com",
        Password = "password123"
    };

    // 2. Act - Call the actual service method
    var result = await _authService.RegisterAsync(request);

    // 3. Assert - Verify the results
    Assert.NotNull(result);
    Assert.Equal("newuser@example.com", result.Email);
    
    // Verify user was created in database
    var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);
    Assert.NotNull(user);
    Assert.Equal("User", user.Role);
}
```

### Controller Tests

These tests verify that controllers handle HTTP requests correctly and return proper responses:

```
┌─────────────────────────────────────────────────────────────┐
│                   Controller Test Flow                       │
├─────────────────────────────────────────────────────────────┤
│  1. Create Mock Service (e.g., Mock<IAuthService>)          │
│           ↓                                                  │
│  2. Create Controller with Mock Service                      │
│           ↓                                                  │
│  3. Setup Mock HTTP Context (authentication claims)          │
│           ↓                                                  │
│  4. Call Controller Action                                   │
│           ↓                                                  │
│  5. Assert HTTP Response (200 OK, 400 Bad Request, etc.)    │
└─────────────────────────────────────────────────────────────┘
```

**Example from TaskControllerTests:**

```csharp
[Fact]
public async Task CreateTask_ValidRequest_ReturnsCreatedAtAction()
{
    // 1. Arrange - Setup mock to return expected response
    var request = new CreateTaskRequest { Title = "New Task", ... };
    var response = new TaskResponse { Id = 1, Title = "New Task", ... };
    
    _taskServiceMock.Setup(x => x.CreateTaskAsync(request, 1, null))
        .ReturnsAsync(response);

    // 2. Act - Call controller action
    var result = await _controller.CreateTask(request, null);

    // 3. Assert - Verify HTTP response
    var createdResult = Assert.IsType<CreatedAtActionResult>(result);
    var returnedTask = Assert.IsType<TaskResponse>(createdResult.Value);
    Assert.Equal("New Task", returnedTask.Title);
}
```

### Data Access Tests (AppDbContextTests)

These tests verify Entity Framework configurations and database operations:

```csharp
[Fact]
public async Task User_EmailMustBeUnique()
{
    // Arrange - Create two users with same email
    var user1 = new User { Email = "same@example.com", ... };
    var user2 = new User { Email = "same@example.com", ... };

    // Act
    _context.Users.Add(user1);
    await _context.SaveChangesAsync();
    _context.Users.Add(user2);

    // Assert - Should throw due to unique constraint
    await Assert.ThrowsAsync<DbUpdateException>(() => 
        _context.SaveChangesAsync());
}
```

### Utility Tests

These tests verify helper classes work correctly:

```csharp
[Fact]
public void VerifyPassword_CorrectPassword_ReturnsTrue()
{
    // Arrange
    var password = "correctPassword123";
    var hashedPassword = PasswordHasher.HashPassword(password);

    // Act
    var result = PasswordHasher.VerifyPassword(password, hashedPassword);

    // Assert
    Assert.True(result);
}
```

---

## Running Tests

### Using Visual Studio
1. Open **Test Explorer** (Test → Test Explorer)
2. Click **Run All Tests** or right-click specific tests

### Using Command Line

```bash
# Navigate to test project
cd Server.Tests

# Run all tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test class
dotnet test --filter "FullyQualifiedName~AuthServiceTests"

# Run specific test method
dotnet test --filter "FullyQualifiedName~RegisterAsync_WithValidRequest"

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Using VS Code
1. Install the **.NET Core Test Explorer** extension
2. Open the Test Explorer panel
3. Run tests from the UI

---

## Understanding Test Coverage

### What We Test

| Layer | What's Tested | Test Files |
|-------|--------------|------------|
| **Services** | Business logic, data operations, validation | `AuthServiceTests.cs`, `TaskServiceTests.cs`, `UserServiceTests.cs` |
| **Controllers** | HTTP request handling, response formatting, authorization | `AuthControllerTests.cs`, `TaskControllerTests.cs`, `UserControllerTests.cs` |
| **Utilities** | Password hashing, JWT generation | `PasswordHasherTests.cs`, `JwtTokenGeneratorTests.cs` |
| **Data Access** | Entity relationships, constraints, queries | `AppDbContextTests.cs` |

### Test Count Summary

| Test File | Number of Tests | Coverage Area |
|-----------|-----------------|---------------|
| AuthServiceTests | 14 | Registration, Login, Email Verification, Password Reset |
| TaskServiceTests | 25 | CRUD operations, Filtering, Statistics, Bulk Operations |
| UserServiceTests | 20 | User management, Stats, Role management |
| AuthControllerTests | 10 | Auth API endpoints |
| TaskControllerTests | 15 | Task API endpoints |
| UserControllerTests | 15 | User management API endpoints |
| PasswordHasherTests | 15 | Password hashing & verification |
| JwtTokenGeneratorTests | 15 | Token generation & claims |
| AppDbContextTests | 18 | Database operations & relationships |

**Total: ~147 tests**

---

## Best Practices

### 1. AAA Pattern (Arrange-Act-Assert)
```csharp
[Fact]
public void TestMethod()
{
    // Arrange - Set up test data and dependencies
    var service = new MyService();
    var input = "test";

    // Act - Execute the code being tested
    var result = service.Process(input);

    // Assert - Verify the results
    Assert.Equal("expected", result);
}
```

### 2. One Assertion Per Concept
Group related assertions, but test one concept per test:

```csharp
// Good - Testing one concept (user creation)
[Fact]
public async Task CreateUser_ValidData_CreatesUserCorrectly()
{
    var result = await service.CreateUser(request);
    
    Assert.NotNull(result);
    Assert.Equal(request.Email, result.Email);
    Assert.Equal("User", result.Role); // All related to the same concept
}
```

### 3. Descriptive Test Names
Use the pattern: `MethodName_Scenario_ExpectedResult`

```csharp
✅ RegisterAsync_WithExistingEmail_ReturnsNull
✅ GetTaskById_AsNonOwner_ReturnsNull
✅ DeleteUser_NonExistentUser_ReturnsFalse

❌ Test1
❌ RegisterTest
❌ ShouldWork
```

### 4. Test Isolation
Each test should be independent:

```csharp
public class MyTests : IDisposable
{
    private readonly AppDbContext _context;

    public MyTests()
    {
        // Fresh database for each test
        _context = TestDbContextFactory.CreateInMemoryContext();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
```

### 5. Use Theory for Multiple Test Cases
```csharp
[Theory]
[InlineData("User")]
[InlineData("Admin")]
[InlineData("SuperAdmin")]
public void GenerateToken_DifferentRoles_ContainsCorrectRole(string role)
{
    var user = new User { Role = role, ... };
    var token = _generator.GenerateToken(user);
    // Assert role is correct
}
```

---

## Common xUnit Commands Reference

| Command | Description |
|---------|-------------|
| `dotnet test` | Run all tests |
| `dotnet test -v n` | Run with normal verbosity |
| `dotnet test -v d` | Run with detailed verbosity |
| `dotnet test --filter "Category=Unit"` | Run tests by category |
| `dotnet test --filter "ClassName~Service"` | Run tests containing "Service" in class name |
| `dotnet test --no-build` | Run without rebuilding |
| `dotnet test --blame` | Run and collect dump on test host crash |

---

## Troubleshooting

### Common Issues

1. **Tests not discovered**
   - Ensure `[Fact]` or `[Theory]` attributes are present
   - Check that test class is `public`
   - Rebuild the solution

2. **In-memory database issues**
   - Use unique database names: `Guid.NewGuid().ToString()`
   - Call `EnsureDeleted()` in `Dispose()`

3. **Mock not working**
   - Ensure `Setup()` matches exact parameters
   - Use `It.IsAny<T>()` for flexible matching

4. **Async test issues**
   - Make sure test method returns `Task`
   - Use `async/await` correctly

---

## Further Reading

- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)
- [Microsoft Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
- [Entity Framework In-Memory Testing](https://docs.microsoft.com/en-us/ef/core/testing/)

---

*Generated for DoSimple Project - January 2026*
