# Sample GraphQL Queries and Mutations for User Service

## Queries

### Get all users
```graphql
query GetUsers {
  users {
    id
    email
    firstName
    lastName
    phoneNumber
    createdAt
    updatedAt
    status
    emailVerified
    profilePictureUrl
    metadata
  }
}
```

### Get user by ID
```graphql
query GetUser($id: UUID!) {
  user(id: $id) {
    id
    email
    firstName
    lastName
    phoneNumber
    createdAt
    updatedAt
    status
    emailVerified
    profilePictureUrl
    metadata
  }
}
```

### Get user by email
```graphql
query GetUserByEmail($email: String!) {
  userByEmail(email: $email) {
    id
    email
    firstName
    lastName
    phoneNumber
    createdAt
    updatedAt
    status
    emailVerified
    profilePictureUrl
    metadata
  }
}
```

## Mutations

### Create new user (Registration)
```graphql
mutation CreateUser($input: CreateUserInput!) {
  createUser(input: $input) {
    id
    email
    firstName
    lastName
    phoneNumber
    createdAt
    status
    emailVerified
    metadata
  }
}
```

#### Variables for CreateUser:
```json
{
  "input": {
    "email": "john.doe@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "phoneNumber": "+1234567890",
    "metadata": {
      "source": "web_registration",
      "campaign": "spring2024"
    }
  }
}
```

### Update user
```graphql
mutation UpdateUser($input: UpdateUserInput!) {
  updateUser(input: $input) {
    id
    email
    firstName
    lastName
    phoneNumber
    updatedAt
    status
    emailVerified
    profilePictureUrl
    metadata
  }
}
```

#### Variables for UpdateUser:
```json
{
  "input": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "firstName": "John Updated",
    "phoneNumber": "+1987654321",
    "profilePictureUrl": "https://example.com/avatar.jpg",
    "metadata": {
      "lastLogin": "2024-01-15T10:30:00Z"
    }
  }
}
```

### Verify email
```graphql
mutation VerifyEmail($userId: UUID!) {
  verifyEmail(userId: $userId) {
    id
    email
    emailVerified
    status
    updatedAt
  }
}
```

#### Variables for VerifyEmail:
```json
{
  "userId": "123e4567-e89b-12d3-a456-426614174000"
}
```

### Delete user
```graphql
mutation DeleteUser($id: UUID!) {
  deleteUser(id: $id)
}
```

#### Variables for DeleteUser:
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000"
}
```

## Health Check
Available at: `GET /health`

## GraphQL Playground
Available at: `/graphql` (when running locally)