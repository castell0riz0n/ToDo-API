# Security Configuration Guide for TeamA.Todo Application

This guide explains how to properly configure the security settings for the TeamA.Todo application in production environments.

## Environment Variables

The application uses environment variables for sensitive configuration to enhance security. Configure the following environment variables on your Windows Server:

### Required Environment Variables

#### JWT_SECRET
The secret key used for signing JWT tokens. This is critical for authentication security.

```powershell
# PowerShell (run as Administrator)
[System.Environment]::SetEnvironmentVariable("JWT_SECRET", "your-very-long-and-secure-secret-key-at-least-32-characters", "Machine")
```

**Requirements:**
- Must be at least 32 characters long
- Should contain a mix of uppercase, lowercase, numbers, and special characters
- Should be unique per environment (dev, staging, production)
- Never commit this value to source control

**Example secure secret generation:**
```powershell
# Generate a secure random string
Add-Type -AssemblyName System.Web
[System.Web.Security.Membership]::GeneratePassword(64, 16)
```

#### ADMIN_EMAIL (Optional)
The email address for the default admin account.

```powershell
[System.Environment]::SetEnvironmentVariable("ADMIN_EMAIL", "admin@yourdomain.com", "Machine")
```

#### ADMIN_PASSWORD (Optional)
The password for the default admin account. If not set, a secure password will be generated and displayed in the logs on first startup.

```powershell
[System.Environment]::SetEnvironmentVariable("ADMIN_PASSWORD", "YourSecureAdminPassword123!", "Machine")
```

**Note:** After setting the admin password, change it immediately after first login.

### Setting Environment Variables on Windows Server

1. **Using System Properties:**
   - Right-click "This PC" → Properties
   - Click "Advanced system settings"
   - Click "Environment Variables"
   - Under "System variables", click "New"
   - Add each variable with its value

2. **Using PowerShell (Recommended):**
   ```powershell
   # Run PowerShell as Administrator
   
   # Set JWT Secret
   [System.Environment]::SetEnvironmentVariable("JWT_SECRET", "your-secret-here", "Machine")
   
   # Set Admin Email
   [System.Environment]::SetEnvironmentVariable("ADMIN_EMAIL", "admin@yourdomain.com", "Machine")
   
   # Set Admin Password (optional)
   [System.Environment]::SetEnvironmentVariable("ADMIN_PASSWORD", "YourSecurePassword123!", "Machine")
   
   # Verify the variables are set
   [System.Environment]::GetEnvironmentVariable("JWT_SECRET", "Machine")
   [System.Environment]::GetEnvironmentVariable("ADMIN_EMAIL", "Machine")
   ```

3. **Using IIS Configuration (for IIS deployments):**
   - Open IIS Manager
   - Select your site
   - Double-click "Configuration Editor"
   - Navigate to system.webServer/aspNetCore
   - Click on environmentVariables
   - Add your environment variables

## HTTPS Configuration

### Enable HTTPS in Production

1. **Obtain an SSL Certificate:**
   - Use Let's Encrypt for free certificates
   - Or purchase from a trusted Certificate Authority

2. **Configure in IIS:**
   - Import the certificate in IIS
   - Bind HTTPS to your site on port 443
   - Configure HTTP to HTTPS redirect

3. **Update appsettings.Production.json:**
   ```json
   {
     "Https": {
       "Port": 443
     },
     "Kestrel": {
       "Endpoints": {
         "Https": {
           "Url": "https://*:443",
           "Certificate": {
             "Path": "path/to/certificate.pfx",
             "Password": "certificate-password"
           }
         }
       }
     }
   }
   ```

### HSTS Configuration

The application automatically enables HTTP Strict Transport Security (HSTS) in production with:
- 365-day max age
- Include subdomains
- Preload enabled

## Rate Limiting

The application implements several rate limiting policies:

1. **Global Rate Limit:**
   - 100 requests per minute per IP/user

2. **Authentication Endpoints:**
   - Login: 5 attempts per 15 minutes
   - Registration: 3 attempts per 24 hours
   - Password Reset: 3 attempts per hour

3. **API Endpoints:**
   - Authenticated users: 200 requests per minute
   - Unauthenticated users: 50 requests per minute

## Security Headers

The application automatically adds the following security headers:
- X-Content-Type-Options: nosniff
- X-Frame-Options: DENY
- X-XSS-Protection: 1; mode=block
- Referrer-Policy: strict-origin-when-cross-origin

## First-Time Setup Checklist

1. [ ] Set JWT_SECRET environment variable
2. [ ] Set ADMIN_EMAIL environment variable (optional)
3. [ ] Set ADMIN_PASSWORD environment variable (optional)
4. [ ] Configure SSL certificate
5. [ ] Enable HTTPS binding in IIS
6. [ ] Configure firewall rules
7. [ ] Start the application
8. [ ] Check logs for generated admin password (if not set)
9. [ ] Login with admin credentials
10. [ ] Change admin password immediately
11. [ ] Configure CORS origins in appsettings.Production.json
12. [ ] Test rate limiting is working

## Monitoring Security

1. **Check Application Logs:**
   - Located in `Logs/` directory
   - Monitor for failed authentication attempts
   - Check for rate limiting violations

2. **Review User Activity:**
   - Use the admin panel to review user activities
   - Monitor for suspicious patterns

3. **Regular Security Updates:**
   - Keep .NET runtime updated
   - Update NuGet packages regularly
   - Review and update security configurations

## Troubleshooting

### JWT Secret Not Found
If you see "JWT Secret is not configured" error:
1. Verify environment variable is set correctly
2. Restart IIS/application pool
3. Check if running under correct user context

### Rate Limiting Too Restrictive
Adjust limits in `ApiExtensions.cs` if needed for your use case.

### HTTPS Redirect Not Working
1. Ensure HTTPS binding is configured in IIS
2. Check firewall allows port 443
3. Verify certificate is valid

## Security Best Practices

1. **Never commit secrets to source control**
2. **Use strong, unique passwords**
3. **Enable two-factor authentication for admin accounts**
4. **Regularly rotate JWT secrets**
5. **Monitor logs for security events**
6. **Keep all dependencies updated**
7. **Use least privilege principle for database connections**
8. **Regular security audits**

## Emergency Procedures

### Compromised JWT Secret
1. Generate new JWT secret immediately
2. Update environment variable
3. Restart application
4. All users will need to re-authenticate

### Suspected Breach
1. Review activity logs
2. Disable compromised accounts
3. Reset all admin passwords
4. Review and rotate all secrets
5. Notify affected users if necessary

For additional security concerns or questions, please contact the development team.
