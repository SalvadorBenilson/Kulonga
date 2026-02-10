#!/bin/bash

# Wait for SQL Server to be ready
echo "Waiting for SQL Server to be ready..."
for i in {1..30}; do
    if timeout 5 bash -c "echo >/dev/tcp/sqlserver/1433" 2>/dev/null; then
        echo "SQL Server is ready!"
        break
    fi
    echo "Attempt $i: Waiting for SQL Server..."
    sleep 2
done

# Start the application
echo "Starting API Gateway..."
exec dotnet api-gateway.dll
