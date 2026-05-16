#!/bin/bash

# DB Seeding Script - Update User Role
# Usage: ./scripts/seed-admin.sh <EMAIL> [ROLE]
# Example: ./scripts/seed-admin.sh admin@example.com SuperAdmin

# Docker container name (from docker-compose.yml)
CONTAINER="starter_db"
DB_NAME="local_db"
DB_USER="local_user"

# Parse arguments
EMAIL="$1"
ROLE="${2:-Admin}"

# Validate inputs
if [ -z "$EMAIL" ]; then
    echo "Error: Email is required"
    echo "Usage: $0 <EMAIL> [ROLE]"
    exit 1
fi

# Validate role
if [[ ! "$ROLE" =~ ^(User|Admin|SuperAdmin)$ ]]; then
    echo "Error: Invalid role '$ROLE'. Must be one of: User, Admin, SuperAdmin"
    exit 1
fi

# Ensure container is running
if ! docker ps --format '{{.Names}}' | grep -q "^${CONTAINER}$"; then
    echo "Error: Docker container '$CONTAINER' is not running. Run: docker-compose up -d"
    exit 1
fi

PSQL="docker exec -i $CONTAINER psql -U $DB_USER -d $DB_NAME -v ON_ERROR_STOP=1"

# SQL-escape the email by doubling any single quotes
SAFE_EMAIL=$(printf '%s' "$EMAIL" | sed "s/'/''/g")
# ROLE is already validated against a fixed whitelist above — safe to embed directly

# Check if user exists
echo "Checking user '$EMAIL'..."
USER_EXISTS=$($PSQL -t -c "SELECT COUNT(*) FROM users WHERE email = '$SAFE_EMAIL';" 2>&1)
if [ $? -ne 0 ]; then
    echo "Error: Failed to query database"
    exit 1
fi
USER_EXISTS=$(echo "$USER_EXISTS" | tr -d ' ')

if [ "$USER_EXISTS" -eq "0" ]; then
    echo "Error: No user found with email '$EMAIL'"
    echo "Hint: Register the account via POST /auth/register first, then re-run this script to elevate the role."
    exit 1
fi

# Update role and capture affected row count via RETURNING
echo "Updating user '$EMAIL' to role '$ROLE'..."
UPDATED=$($PSQL -t \
    -c "UPDATE users SET role_type = '$ROLE', updated_at = CURRENT_TIMESTAMP WHERE email = '$SAFE_EMAIL' RETURNING id;" 2>&1)
if [ $? -ne 0 ]; then
    echo "Error: Failed to execute update"
    exit 1
fi

UPDATED=$(echo "$UPDATED" | grep -c '[0-9a-f-]\{36\}' || true)
if [ "$UPDATED" -ge "1" ]; then
    echo "Success: User '$EMAIL' role updated to '$ROLE'"
    exit 0
else
    echo "Error: Failed to update user role"
    exit 1
fi
