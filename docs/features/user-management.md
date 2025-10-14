# User Profile API Implementation

## Overview
This document summarizes the implementation of User Profile API endpoints for the Game Server Hosting platform as specified in Linear issue MOS-18.

## Implemented Endpoints

### 1. GET /api/users/profile
- **Description**: Retrieves the current authenticated user's profile information
- **Authentication**: Required (JWT Bearer token)
- **Response**: `UserProfileResponse` containing user details
- **Status Codes**:
  - 200 OK: Profile retrieved successfully
  - 401 Unauthorized: User not authenticated
  - 404 Not Found: User not found
  - 500 Internal Server Error: Server error

### 2. PUT /api/users/profile
- **Description**: Updates the current authenticated user's profile information
- **Authentication**: Required (JWT Bearer token)
- **Request Body**: `UpdateProfileRequest`
  - FirstName (required, 1-50 characters)
  - LastName (required, 1-50 characters)
  - Email (required, valid email format)
  - PhoneNumber (optional, valid phone format)
- **Response**: `UserProfileResponse` with updated information
- **Special Behaviors**:
  - If email is changed, `emailConfirmed` is set to false (requires re-verification)
  - Prevents email conflicts with existing users
- **Status Codes**:
  - 200 OK: Profile updated successfully
  - 400 Bad Request: Validation errors or email already in use
  - 401 Unauthorized: User not authenticated
  - 404 Not Found: User not found
  - 500 Internal Server Error: Server error

### 3. PUT /api/users/change-password
- **Description**: Changes the current authenticated user's password
- **Authentication**: Required (JWT Bearer token)
- **Request Body**: `ChangePasswordRequest`
  - CurrentPassword (required)
  - NewPassword (required, min 8 chars, must contain uppercase, lowercase, digit, and special character)
  - ConfirmPassword (required, must match NewPassword)
- **Response**: Success message
- **Status Codes**:
  - 200 OK: Password changed successfully
  - 400 Bad Request: Current password incorrect or validation errors
  - 401 Unauthorized: User not authenticated
  - 404 Not Found: User not found
  - 500 Internal Server Error: Server error

### 4. DELETE /api/users/account
- **Description**: Deletes (soft delete) the current authenticated user's account
- **Authentication**: Required (JWT Bearer token)
- **Response**: Success message
- **Special Behaviors**:
  - Implements soft delete by setting `isActive` to false
  - Preserves data for integrity purposes
  - User can no longer log in after deletion
- **Status Codes**:
  - 200 OK: Account deleted successfully
  - 401 Unauthorized: User not authenticated
  - 404 Not Found: User not found
  - 500 Internal Server Error: Server error

## Files Created

### Models
1. **src/GameServerHosting.Api/Models/UpdateProfileRequest.cs**
   - Request model for profile updates
   - Includes validation attributes

2. **src/GameServerHosting.Api/Models/ChangePasswordRequest.cs**
   - Request model for password changes
   - Includes password complexity validation
   - Includes password confirmation matching

3. **src/GameServerHosting.Api/Models/UserProfileResponse.cs**
   - Response model for user profile data
   - Includes all relevant user information

### Controllers
4. **src/GameServerHosting.Api/Controllers/UsersController.cs**
   - Main controller implementing all four endpoints
   - Includes proper authorization, logging, and error handling
   - Follows existing code patterns from AuthController

### Testing
5. **src/GameServerHosting.Api/user-profile-tests.http**
   - Comprehensive HTTP test file
   - Includes setup instructions
   - Tests all endpoints with various scenarios
   - Compatible with VS Code REST Client extension

## Security Features

1. **Authentication**: All endpoints require JWT Bearer token authentication
2. **Authorization**: Users can only access/modify their own profile
3. **Password Validation**: Strong password requirements enforced
4. **Email Verification**: Email changes require re-verification
5. **Soft Delete**: Account deletion preserves data integrity
6. **Logging**: All operations are logged for audit purposes

## Integration with Existing System

The implementation integrates seamlessly with the existing codebase:
- Uses ASP.NET Core Identity (`UserManager<ApplicationUser>`)
- Follows the same patterns as `AuthController`
- Uses the existing `ApplicationUser` entity
- Integrates with existing JWT authentication
- Follows the same logging conventions
- Uses consistent error handling and response formats

## Testing Instructions

1. Start the API server
2. Use the `user-profile-tests.http` file with VS Code REST Client extension
3. Follow the setup steps to:
   - Register a new user
   - Verify email
   - Login to get access token
4. Test each endpoint with the provided examples
5. Verify responses match expected formats

## Notes

- All endpoints return JSON responses
- Swagger documentation is automatically generated from XML comments
- The implementation follows RESTful API best practices
- Error messages are user-friendly and informative
- Validation is performed both at model level and in controller logic

## Future Enhancements (Optional)

- Add support for profile picture uploads
- Implement two-factor authentication
- Add account recovery options
- Implement hard delete with data export
- Add rate limiting for sensitive operations
# User Settings Page Implementation

## Summary

Successfully implemented a comprehensive User Settings Page for the Game Server Hosting application with the following features:

### ✅ Features Implemented

1. **Profile Editing Form**
   - First Name and Last Name fields
   - Email address (with email verification status indicator)
   - Phone number (optional)
   - Account metadata display (created date, last updated, last login)
   - Real-time validation
   - Success/error messaging

2. **Password Change Form**
   - Current password verification
   - New password with strength requirements
   - Password confirmation
   - Validation rules (minimum 8 characters, uppercase, lowercase, digit, special character)
   - Security tips section

3. **Email Preferences**
   - Promotional emails toggle
   - Order updates toggle
   - Server alerts toggle
   - Newsletter toggle
   - Auto-save on toggle change

4. **Notification Settings**
   - Browser notifications toggle
   - Server offline alerts toggle
   - Resource alerts toggle
   - Payment due reminders toggle
   - Security alerts toggle
   - Auto-save on toggle change

### 📁 Files Created

#### Backend (API)
- `src/GameServerHosting.Shared/Entities/UserPreferences.cs` - Entity model for user preferences
- `src/GameServerHosting.Api/Models/UserPreferencesDto.cs` - DTO for preferences API

#### Frontend (Client)
- `src/GameServerHosting.Client/Models/UpdateProfileRequest.cs` - Profile update request model
- `src/GameServerHosting.Client/Models/ChangePasswordRequest.cs` - Password change request model
- `src/GameServerHosting.Client/Models/UserPreferencesDto.cs` - Preferences DTO
- `src/GameServerHosting.Client/Services/UserService.cs` - Service for user-related API calls
- `src/GameServerHosting.Client/Pages/Settings.razor` - Main settings page component
- `src/GameServerHosting.Client/Pages/Settings.razor.css` - Styling for settings page

### 🔧 Files Modified

#### Backend
- `src/GameServerHosting.Api/Controllers/UsersController.cs`
  - Added `GetPreferences()` endpoint (GET `/api/users/preferences`)
  - Added `UpdatePreferences()` endpoint (PUT `/api/users/preferences`)

- `src/GameServerHosting.Api/Data/GameServerDbContext.cs`
  - Added `UserPreferences` DbSet
  - Added UserPreferences entity configuration with proper column mappings

#### Frontend
- `src/GameServerHosting.Client/Program.cs`
  - Registered `UserService` in DI container

- `src/GameServerHosting.Client/Layout/NavMenu.razor`
  - Added "Settings" navigation link with gear icon

### 🎨 Design Features

- **Modern UI**: Tabbed interface for easy navigation between different settings categories
- **Responsive Design**: Mobile-friendly layout that adapts to different screen sizes
- **Consistent Styling**: Matches the existing Dashboard design with gradient accents
- **User Feedback**: Success/error messages with auto-dismiss for preferences
- **Real-time Updates**: Profile metadata displayed (created date, last login, etc.)
- **Accessibility**: Proper form labels, ARIA attributes, and keyboard navigation

### 🗄️ Database Migration Required

A new `user_preferences` table needs to be created in the database. Run the following commands:

```bash
# Navigate to the API project
cd src/GameServerHosting.Api

# Create a new migration
dotnet ef migrations add AddUserPreferences

# Apply the migration to the database
dotnet ef database update
```

Alternatively, you can run this SQL directly:

```sql
CREATE TABLE user_preferences (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL,
    email_promotional BOOLEAN NOT NULL DEFAULT true,
    email_order_updates BOOLEAN NOT NULL DEFAULT true,
    email_server_alerts BOOLEAN NOT NULL DEFAULT true,
    email_newsletter BOOLEAN NOT NULL DEFAULT true,
    notifications_browser BOOLEAN NOT NULL DEFAULT true,
    notifications_server_offline BOOLEAN NOT NULL DEFAULT true,
    notifications_resource_alerts BOOLEAN NOT NULL DEFAULT true,
    notifications_payment_due BOOLEAN NOT NULL DEFAULT true,
    notifications_security_alerts BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_user_preferences_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX idx_user_preferences_user_id ON user_preferences(user_id);
```

### 📋 API Endpoints

#### Existing Endpoints Used
- `GET /api/users/profile` - Get user profile
- `PUT /api/users/profile` - Update user profile
- `PUT /api/users/change-password` - Change password

#### New Endpoints Added
- `GET /api/users/preferences` - Get user preferences (auto-creates defaults if not exist)
- `PUT /api/users/preferences` - Update user preferences

### 🔐 Security Features

- All endpoints require authentication (`[Authorize]` attribute)
- Password validation with complexity requirements
- Current password verification before allowing password change
- Email change triggers re-verification requirement
- Preferences are user-specific and isolated

### 🚀 Usage

1. **Access Settings**: Navigate to `/settings` or click "Settings" in the navigation menu
2. **Edit Profile**: Update your personal information in the "Profile" tab
3. **Change Password**: Securely update your password in the "Security" tab
4. **Email Preferences**: Toggle email notification preferences in the "Email Preferences" tab
5. **Notifications**: Manage notification settings in the "Notifications" tab

### 🧪 Testing Recommendations

1. **Profile Update**
   - Test with valid and invalid email formats
   - Verify email confirmation status changes when email is updated
   - Test phone number validation

2. **Password Change**
   - Test with incorrect current password
   - Test with weak new password
   - Test password mismatch scenarios
   - Verify success message and form reset

3. **Preferences**
   - Toggle each preference and verify auto-save
   - Verify preferences persist across page refreshes
   - Test default preferences for new users

### 📱 Responsive Breakpoints

- Desktop: Full layout with side-by-side elements
- Tablet (≤768px): Stacked layout, scrollable tabs
- Mobile: Optimized for touch, full-width components

### 🎯 Future Enhancements

Potential improvements for future iterations:
- Two-factor authentication setup
- Account deletion/deactivation
- Profile picture upload
- Timezone and language preferences
- API key management
- Session management (view active sessions)
- Export user data (GDPR compliance)
- Dark mode preference

## Notes

- The API automatically creates default preferences when a user first accesses the preferences endpoint
- All toggle changes in Email and Notification preferences are auto-saved
- Profile and Password changes require explicit "Save" button clicks
- Success messages for preferences auto-dismiss after 3 seconds
