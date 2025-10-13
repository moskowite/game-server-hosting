# Quick Start Guide - GameHost Marketplace

## 📋 What You Have

1. **GAME_SERVER_MARKETPLACE_ROADMAP.md** - Comprehensive project roadmap with detailed descriptions
2. **LINEAR_IMPORT.csv** - CSV file ready for Linear import
3. **This Quick Start Guide** - Getting started instructions

## 🚀 Getting Started with Linear

### Step 1: Import Tasks into Linear

#### Option A: CSV Import (Recommended)
1. Open Linear workspace
2. Click on your team/project
3. Go to Settings → Import
4. Select "CSV Import"
5. Upload `LINEAR_IMPORT.csv`
6. Map columns:
   - Title → Title
   - Description → Description
   - Priority → Priority (create custom field if needed)
   - Status → Status
   - Estimate → Estimate
   - Epic → Parent Issue or Label
   - Phase → Label
   - Labels → Labels
7. Review and confirm import

#### Option B: Manual Creation
1. Create milestones for each Phase (1-13)
2. Create parent issues for each Epic
3. Copy tasks from roadmap one by one
4. Set priorities and estimates

### Step 2: Configure Linear Workspace

#### Create Custom Fields
- **Priority**: Dropdown (P0-Critical, P1-High, P2-Medium, P3-Low)
- **Epic**: Relationship (links to parent issue)
- **Phase**: Label
- **Estimate**: Number (hours)

#### Create Labels
**By Type:**
- Backend
- Frontend
- Database
- DevOps
- Design
- Testing
- Documentation

**By Phase:**
- Phase 1: Foundation
- Phase 2: Auth
- Phase 3: Catalog
- Phase 4: Cart
- Phase 5: Payments
- Phase 6: Management
- Phase 7: Admin
- Phase 8: Billing
- Phase 9: Notifications
- Phase 10: Polish
- Phase 11: Security
- Phase 12: Testing
- Phase 13: Launch

#### Setup Workflow States
1. **Backlog** - Not yet scheduled
2. **Todo** - Ready to start
3. **In Progress** - Currently being worked on
4. **In Review** - Awaiting review/testing
5. **Done** - Completed

### Step 3: Organize Your Team

#### Assign Roles
- **Backend Developer(s)**: Tasks labeled "Backend"
- **Frontend Developer(s)**: Tasks labeled "Frontend"
- **DevOps Engineer**: Tasks labeled "DevOps"
- **QA/Tester**: Tasks labeled "Testing"

#### Setup Views
1. **By Phase**: Group by Phase label
2. **By Priority**: Filter and sort by priority
3. **By Assignee**: Group by team member
4. **Sprint View**: Current 2-week sprint
5. **My Tasks**: Personal view

## 🤖 Working with Cursor Agent

### Best Practices for Task Delegation

#### 1. **Be Specific and Clear**
❌ Bad: "Create the auth system"
✅ Good: "Implement the JWT authentication service. Create a new TokenService class in the Services folder that generates JWT tokens using ASP.NET Core Identity user data. Include methods for GenerateToken, ValidateToken, and RefreshToken. Use configuration from appsettings.json for the secret key."

#### 2. **Provide Context**
Always include:
- What files to modify or create
- What existing code to reference
- Expected input/output
- Dependencies on other components

#### 3. **Break Down Large Tasks**
If a task seems too complex, break it into smaller sub-tasks:
- Original: "Implement Server Management API" (5 hours)
- Split into:
  - Create ServerController (1 hour)
  - Implement GET endpoints (1 hour)
  - Implement POST/PUT/DELETE endpoints (1 hour)
  - Add validation and error handling (1 hour)
  - Write unit tests (1 hour)

### Example Prompts for Cursor Agent

#### Backend Development

**Task: Create Authentication API**
```
Create an AuthController in the Controllers folder with the following specifications:

1. POST /api/auth/register
   - Accept: RegisterDto (Email, Password, ConfirmPassword, FirstName, LastName)
   - Validate input using data annotations
   - Use UserManager to create user
   - Return JWT token on success

2. POST /api/auth/login
   - Accept: LoginDto (Email, Password)
   - Use SignInManager to validate credentials
   - Generate JWT token using TokenService
   - Return token and user info

3. POST /api/auth/refresh-token
   - Accept: RefreshTokenDto (RefreshToken)
   - Validate refresh token
   - Generate new access token
   - Return new tokens

Include proper error handling with appropriate HTTP status codes.
Add XML documentation comments for Swagger.
```

**Task: Create Entity Models**
```
Create the following Entity Framework Core entities in the Models folder:

1. ServerType.cs
   - Properties: Id (Guid), Name (string), Description (string), IconUrl (string), IsActive (bool)
   - Navigation: Collection of ServerPlans

2. ServerPlan.cs
   - Properties: Id (Guid), Name (string), ServerTypeId (Guid), RAM (int), CPU (int), Slots (int), Storage (int), PriceMonthly (decimal), PriceYearly (decimal)
   - Navigation: ServerType, Collection of GameServers

3. GameServer.cs
   - Properties: Id (Guid), UserId (string), ServerPlanId (Guid), ServerName (string), Status (ServerStatus enum), IpAddress (string), Port (int), CreatedAt (DateTime), ExpiresAt (DateTime)
   - Navigation: User, ServerPlan

Add proper data annotations for validation.
Configure relationships using Fluent API in a DbContext extension method.
```

#### Frontend Development

**Task: Create Server Catalog Component**
```
Create a Blazor component called ServerCatalog.razor in the Pages folder:

1. UI Requirements:
   - Responsive grid layout (3 columns on desktop, 1 on mobile)
   - Each card shows: Server icon, name, description, starting price
   - Hover effect: slight elevation and scale
   - Click navigates to server details page

2. Functionality:
   - Fetch server types from /api/server-types on load
   - Show loading skeleton while fetching
   - Handle errors gracefully with error message
   - Use MudBlazor components (MudGrid, MudCard)

3. State Management:
   - Use HttpClient to fetch data
   - Store in local component state
   - Show loading, error, and data states

Include proper error handling and loading states.
```

**Task: Implement Shopping Cart**
```
Create a cart state management service:

1. Create CartService.cs in Services folder:
   - AddToCart(ServerPlan plan, ServerConfiguration config)
   - RemoveFromCart(Guid itemId)
   - UpdateQuantity(Guid itemId, int quantity)
   - ClearCart()
   - GetCart() - returns current cart items
   - GetTotal() - calculates total price

2. Persist cart to browser localStorage
3. Raise events when cart changes
4. Make it a scoped service

Then create CartComponent.razor:
- Display cart items
- Show quantities and prices
- Remove item buttons
- Total price calculation
- Checkout button
```

#### Database Tasks

**Task: Create Database Migration**
```
Create an Entity Framework Core migration for the server management entities:

1. Run: dotnet ef migrations add InitialServerEntities
2. Review the generated migration
3. Add any custom SQL if needed (indexes, etc.)
4. Test migration on local database: dotnet ef database update
5. Create rollback plan in case of issues

Verify these tables are created:
- ServerTypes
- ServerPlans
- GameServers
- Orders
- Payments
```

#### Testing Tasks

**Task: Write Unit Tests**
```
Create unit tests for the AuthService:

1. Create AuthService.Tests.cs in Tests/Services folder
2. Use xUnit framework
3. Mock dependencies (UserManager, SignInManager, TokenService)
4. Test cases:
   - RegisterUser_ValidInput_ReturnsSuccess
   - RegisterUser_DuplicateEmail_ReturnsError
   - Login_ValidCredentials_ReturnsToken
   - Login_InvalidCredentials_ReturnsError
   - RefreshToken_ValidToken_ReturnsNewToken
   - RefreshToken_ExpiredToken_ReturnsError

Use Moq for mocking and FluentAssertions for assertions.
Aim for 100% code coverage of AuthService.
```

### Task Workflow with Cursor

#### Phase 1: Planning
1. Pick a task from Linear (start with "Todo" status)
2. Move to "In Progress"
3. Read the task description thoroughly
4. Identify dependencies

#### Phase 2: Implementation
1. Open Cursor in the project directory
2. Use specific prompts to implement the feature
3. Review generated code
4. Test locally
5. Fix any issues

#### Phase 3: Review
1. Run linter and fix warnings
2. Run unit tests
3. Test manually
4. Create pull request
5. Move task to "In Review" in Linear

#### Phase 4: Completion
1. Address review comments
2. Merge PR
3. Move task to "Done" in Linear
4. Update any dependent tasks

## 📅 Sprint Planning

### Recommended Sprint Structure

#### Sprint 0 (Week 1-2): Setup
- Tasks 1-8: Foundation & Database
- Goal: Working development environment with database

#### Sprint 1 (Week 2-3): Authentication
- Tasks 9-15: User authentication and profiles
- Goal: Users can register, login, and manage profiles

#### Sprint 2 (Week 3-4): Server Catalog
- Tasks 16-22: Server browsing and selection
- Goal: Users can browse and view server options

#### Sprint 3 (Week 4-5): Cart & Checkout
- Tasks 23-29: Shopping cart and checkout flow
- Goal: Users can add to cart and place orders

#### Sprint 4 (Week 5-6): Payments
- Tasks 30-34: Stripe integration
- Goal: Users can complete purchases

#### Sprint 5-6 (Week 6-8): Server Management
- Tasks 35-43: Provisioning and control panel
- Goal: Servers are provisioned and users can manage them

#### Sprint 7 (Week 8-9): Admin Panel
- Tasks 44-48: Admin functionality
- Goal: Admins can manage users, servers, and orders

#### Sprint 8 (Week 9-10): Billing
- Tasks 49-53: Recurring billing and subscriptions
- Goal: Automated billing system working

#### Sprint 9 (Week 10-11): Notifications
- Tasks 54-60: Notifications and support
- Goal: Users receive notifications and can get support

#### Sprint 10 (Week 11-12): Polish
- Tasks 61-68: UI/UX and performance
- Goal: Production-ready performance and UX

#### Sprint 11 (Week 12-13): Security
- Tasks 69-76: Security hardening and compliance
- Goal: Secure and compliant system

#### Sprint 12-13 (Week 13-14): Testing
- Tasks 77-82: Comprehensive testing
- Goal: Fully tested, bug-free application

#### Sprint 14 (Week 14-15): Launch
- Tasks 83-93: Deployment and launch
- Goal: Public launch!

## 🏗️ Technical Setup

### Initialize New Project

#### Step 1: Create Solution
```bash
# Create a new directory for your project
mkdir GameHostMarketplace
cd GameHostMarketplace

# Create solution
dotnet new sln -n GameHostMarketplace

# Create Web API project
dotnet new webapi -n GameHostMarketplace.API
dotnet sln add GameHostMarketplace.API

# Create Blazor WebAssembly project
dotnet new blazorwasm -n GameHostMarketplace.Web
dotnet sln add GameHostMarketplace.Web

# Create class library for shared models
dotnet new classlib -n GameHostMarketplace.Shared
dotnet sln add GameHostMarketplace.Shared

# Create test projects
dotnet new xunit -n GameHostMarketplace.API.Tests
dotnet sln add GameHostMarketplace.API.Tests
```

#### Step 2: Install Packages

**API Project:**
```bash
cd GameHostMarketplace.API

# Entity Framework Core
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

# ASP.NET Core Identity
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore

# JWT Authentication
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer

# Stripe
dotnet add package Stripe.net

# Logging
dotnet add package Serilog.AspNetCore

# Background Jobs
dotnet add package Hangfire
dotnet add package Hangfire.PostgreSql

# SignalR (included in ASP.NET Core)

cd ..
```

**Blazor Project:**
```bash
cd GameHostMarketplace.Web

# HTTP Client
dotnet add package Microsoft.Extensions.Http

# Blazor Component Library
dotnet add package MudBlazor

# Local Storage
dotnet add package Blazored.LocalStorage

# State Management
dotnet add package Fluxor.Blazor.Web

cd ..
```

**Test Project:**
```bash
cd GameHostMarketplace.API.Tests

dotnet add package Moq
dotnet add package FluentAssertions
dotnet add package Microsoft.EntityFrameworkCore.InMemory

cd ..
```

#### Step 3: Project References
```bash
# API references Shared
dotnet add GameHostMarketplace.API/GameHostMarketplace.API.csproj reference GameHostMarketplace.Shared/GameHostMarketplace.Shared.csproj

# Web references Shared
dotnet add GameHostMarketplace.Web/GameHostMarketplace.Web.csproj reference GameHostMarketplace.Shared/GameHostMarketplace.Shared.csproj

# Tests reference API
dotnet add GameHostMarketplace.API.Tests/GameHostMarketplace.API.Tests.csproj reference GameHostMarketplace.API/GameHostMarketplace.API.csproj
```

### Database Setup

#### Install PostgreSQL Locally
**Windows:**
```powershell
# Using chocolatey
choco install postgresql

# Or download from: https://www.postgresql.org/download/windows/
```

**Create Database:**
```bash
psql -U postgres
CREATE DATABASE gamehostmarketplace;
\q
```

#### Connection String
Add to `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=gamehostmarketplace;Username=postgres;Password=your_password"
  }
}
```

### Git Setup

```bash
# Initialize git
git init

# Create .gitignore (download .NET template)
curl -o .gitignore https://raw.githubusercontent.com/github/gitignore/main/VisualStudio.gitignore

# Add secrets to ignore
echo "appsettings.Development.json" >> .gitignore
echo "*.env" >> .gitignore

# Initial commit
git add .
git commit -m "Initial project setup"

# Create GitHub repo and push
git remote add origin https://github.com/yourusername/GameHostMarketplace.git
git branch -M main
git push -u origin main
```

## 📊 Tracking Progress

### Daily Standup Questions
1. What did I complete yesterday?
2. What am I working on today?
3. Any blockers?

### Weekly Review
- Review completed tasks in Linear
- Update roadmap if needed
- Adjust sprint plan
- Demo progress to stakeholders

### Metrics to Track
- **Velocity**: Story points completed per sprint
- **Quality**: Bugs found vs fixed
- **Coverage**: Test coverage percentage
- **Performance**: API response times
- **Technical Debt**: Accumulated issues

## 🎯 Success Criteria

### Phase 1 Complete When:
- ✅ Solution structure created
- ✅ Database connected and migrations working
- ✅ CI/CD pipeline running
- ✅ Core entities defined

### Phase 2 Complete When:
- ✅ Users can register and login
- ✅ JWT authentication working
- ✅ User dashboard displaying data

### MVP Complete When:
- ✅ Users can browse servers
- ✅ Users can complete purchase
- ✅ Payment processing works
- ✅ Servers are provisioned
- ✅ Users can manage servers

### Launch Ready When:
- ✅ All tests passing
- ✅ Security audit complete
- ✅ Performance meets targets
- ✅ Documentation complete
- ✅ Production environment ready

## 🆘 Troubleshooting

### Common Issues with Cursor Agent

**Issue: Code doesn't compile**
- Solution: Ask Cursor to "fix the compilation errors" with context

**Issue: Missing dependencies**
- Solution: Check package references and NuGet packages

**Issue: Tests failing**
- Solution: Review test setup and mock configurations

**Issue: Database migration errors**
- Solution: Drop database and recreate, or fix migration manually

### Getting Help

1. **Project Documentation**: Refer to roadmap for details
2. **Linear Comments**: Ask questions on tasks
3. **Cursor Agent**: Ask for clarification or fixes
4. **Community**: Stack Overflow, Reddit r/dotnet, Discord communities

## 🚀 Ready to Start?

### Your First Day Checklist:
- [ ] Import tasks into Linear
- [ ] Setup Linear workspace (labels, milestones, views)
- [ ] Create new project directory
- [ ] Initialize .NET solution with projects
- [ ] Install required NuGet packages
- [ ] Setup PostgreSQL database
- [ ] Initialize Git repository
- [ ] Pick first task from Sprint 0
- [ ] Open Cursor and start coding!

### First Task Recommendation:
**Start with Task #1: "Setup Solution Structure"**

This gives you a solid foundation to build on. Use this Cursor prompt:

```
Create a new .NET 8 solution for a game server marketplace application called "GameHostMarketplace" with the following structure:

1. Create a solution file
2. Add ASP.NET Core Web API project named "GameHostMarketplace.API"
3. Add Blazor WebAssembly project named "GameHostMarketplace.Web"
4. Add class library named "GameHostMarketplace.Shared" for DTOs and shared models
5. Add xUnit test project named "GameHostMarketplace.API.Tests"
6. Setup project references appropriately
7. Create folder structure in API project: Controllers, Models, Services, Data
8. Create folder structure in Web project: Pages, Components, Services, Models

Use .NET 8.0 and follow best practices for project organization.
```

## 📞 Support

If you need help adapting this roadmap or have questions about implementation, remember:
- Cursor Agent can help with code implementation
- Linear can help with project tracking
- This guide can help with overall project direction

**Good luck building your GameHost Marketplace! 🎮🚀**

