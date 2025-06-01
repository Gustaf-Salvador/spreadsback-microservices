#!/bin/bash

# Build and deploy script for User Service Lambda

echo "Building User Service..."
dotnet build --configuration Release

if [ $? -eq 0 ]; then
    echo "Build successful!"
    
    echo "Publishing for Lambda deployment..."
    dotnet publish --configuration Release --framework net8.0 --output ./publish
    
    echo "Creating deployment package..."
    cd publish
    zip -r ../user-service-lambda.zip .
    cd ..
    
    echo "Deployment package created: user-service-lambda.zip"
    
    if [ "$1" == "deploy" ]; then
        echo "Deploying to AWS Lambda..."
        sam deploy --guided
    fi
else
    echo "Build failed!"
    exit 1
fi