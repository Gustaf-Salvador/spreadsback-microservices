# User Service - .NET Core GraphQL Microservice

This microservice handles user registration workflow data and provides a GraphQL API for user management operations.

## Features

- **GraphQL API** with queries and mutations for user operations
- **AWS Lambda** deployment ready
- **Entity Framework Core** with in-memory database
- **Repository pattern** for data access
- **Input validation** and error handling
- **Health checks** endpoint
- **CORS** configuration for web client integration

## API Operations

### Queries
- `getUser(id: ID!)` - Get user by ID
- `getUserByEmail(email: String!)` - Get user by email
- `getUsers` - Get all users

### Mutations
- `createUser(input: CreateUserInput!)` - Register new user
- `updateUser(input: UpdateUserInput!)` - Update user information
- `verifyEmail(userId: ID!)` - Verify user email and activate account
- `deleteUser(id: ID!)` - Delete user

## User Model

The service manages users with the following properties:
- Basic info: email, firstName, lastName, phoneNumber
- Status tracking: status (Pending/Active/Suspended/Deactivated)
- Email verification: emailVerified boolean
- Timestamps: createdAt, updatedAt
- Flexible metadata: JSON object for additional data
- Profile picture URL

## Deployment

The service is configured for AWS Lambda deployment using AWS SAM template. The `template.yaml` file defines the Lambda function and API Gateway integration.

## Development

To run locally:
```bash
dotnet restore
dotnet run
```

GraphQL endpoint will be available at `/graphql` with GraphQL Playground for testing.

## Integration

This microservice is designed to integrate with the Spreadsback app registration workflow, receiving user data from the frontend and storing it for further processing.