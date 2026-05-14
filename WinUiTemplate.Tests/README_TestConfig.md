# Test Configuration Setup

This directory contains test configuration files for running integration tests with PostgreSQL.

## Setup Instructions

1. **Copy the example configuration file:**
   ```bash
   cp testconfig.example.json testconfig.json
   ```

2. **Edit `testconfig.json` with your PostgreSQL credentials:**
   ```json
   {
     "PostgreSQL": {
       "Host": "192.168.1.2",
       "Port": 5432,
       "Database": "WinUI3Debug",
       "Username": "your_actual_username",
       "Password": "your_actual_password"
     }
   }
   ```

3. **Important:** 
   - `testconfig.json` is git-ignored to prevent credentials from being committed
   - The tests will create temporary databases with names like `WinUI3Debug_test_<guid>`
   - These temporary databases are automatically cleaned up after tests complete
   - Make sure your PostgreSQL user has permissions to CREATE and DROP databases

## Required PostgreSQL Permissions

Your PostgreSQL user needs the following permissions:
- CREATE DATABASE
- DROP DATABASE
- CREATE TABLE
- SELECT, INSERT, UPDATE, DELETE on created tables

## Test Database Behavior

- The main database (`WinUI3Debug` in your case) is used as the connection database
- Each test run creates unique temporary databases to avoid conflicts
- Temporary databases are named: `<Database>_test_<guid>`
- All temporary databases are dropped automatically after tests complete

## Troubleshooting

If tests fail with connection errors:
1. Verify PostgreSQL is running at the configured host and port
2. Check that the database exists (create it if needed)
3. Verify the username and password are correct
4. Ensure the user has CREATE/DROP database permissions
5. Check firewall rules if using a remote PostgreSQL server
