# Liara Deployment Guide for PicoPlus

This guide will help you deploy your .NET 9 Blazor application to Liara platform.

## Prerequisites

1. **Liara CLI installed**
   ```bash
   npm install -g @liara/cli
   ```

2. **Environment Variables** (Set in Liara Dashboard)
   - `HUBSPOT_TOKEN`: Your HubSpot API token
   - `ZIBAL_TOKEN`: Your Zibal API token
   - `ASPNETCORE_ENVIRONMENT`: Production
   - `ASPNETCORE_URLS`: http://+:5000

## Project Configuration

### Files Created/Updated

1. **liara.json** - Liara platform configuration
   - Port: 5000 (matches ASP.NET Core)
   - Platform: Docker
   - Environment variables configured

2. **Dockerfile** - Optimized multi-stage build
   - Build stage: .NET 9 SDK
   - Runtime stage: .NET 9 ASP.NET Runtime
   - Optimized for Iranian networks

3. **.dockerignore** - Reduces build context size
   - Excludes build artifacts, secrets, and unnecessary files

4. **Program.cs** - Production optimizations
   - HTTP-only in production (no HTTPS certificate needed)
   - Response compression enabled
   - Static file caching (30 days)
   - Optimized logging
   - Data Protection keys persisted to filesystem

5. **docker-compose.yml** - Local testing
   - Test production build locally before deploying

6. **DataProtectionKeys/** - Persistent encryption keys
   - Stores ASP.NET Core data protection keys
   - Must be persisted across deployments
   - Excluded from git (.gitignore)

## Deployment Steps

### 1. Login to Liara

```bash
liara login
```

### 2. Set Environment Variables in Liara Dashboard

Go to your app settings in Liara dashboard and add:

```
HUBSPOT_TOKEN=your_hubspot_token_here
ZIBAL_TOKEN=your_zibal_token_here
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000
```

### 3. Deploy to Liara

```bash
# From project root directory
liara deploy
```

The deployment process will:
1. Upload your code to Liara
2. Build Docker image using multi-stage Dockerfile
3. Run the container on port 5000
4. Map persistent disk to /app/data

**Important**: Data protection keys are stored in `DataProtectionKeys/` directory. In production:
- Keys are automatically created on first run
- Keys are persisted to prevent antiforgery token errors
- Ensure the directory has proper write permissions in container
- If using Liara persistent disk, mount to /app for key persistence

### 4. Monitor Logs

```bash
liara logs --app ipicoplus
```

## Local Testing (Before Deployment)

### Test with Docker Compose

1. Create `.env` file in project root:
   ```
   HUBSPOT_TOKEN=your_token
   ZIBAL_TOKEN=your_token
   ```

2. Build and run:
   ```bash
   docker-compose up --build
   ```

3. Access at: http://localhost:5000

### Test Docker Build Only

```bash
# Build image
docker build -t picoplus:test .

# Run container
docker run -p 5000:5000 \
  -e HUBSPOT_TOKEN=your_token \
  -e ZIBAL_TOKEN=your_token \
  picoplus:test
```

## Production Optimizations Applied

### Performance
- ? Response compression (Brotli/Gzip)
- ? Static file caching (30 days)
- ? Optimized logging (Warning level)
- ? Multi-stage Docker build (smaller image)

### Security
- ? HTTP-only (Liara handles HTTPS termination)
- ? Environment variables for secrets
- ? No sensitive data in image
- ? HSTS enabled in production

### Scalability
- ? Stateless application design
- ? Memory cache configured
- ? Persistent disk for data
- ? Blazor Server with SignalR

## Troubleshooting

### Issue: Port binding error
**Solution**: Liara automatically maps external port 80/443 to internal port 5000

### Issue: Environment variables not working
**Solution**: Check Liara dashboard settings, not `.env` file

### Issue: Build fails
**Solution**: 
```bash
# Clean and rebuild locally first
dotnet clean
dotnet build -c Release
```

### Issue: Connection to HubSpot fails
**Solution**: Shecan DNS handler is configured for Iranian networks. Check firewall rules.

### Issue: Antiforgery token errors after restart
**Solution**: Data protection keys are now persisted to `DataProtectionKeys/` directory. Ensure this directory is included in persistent storage or mounted volume.

## Monitoring

### Check Application Health
```bash
curl http://your-app.liara.run/
```

### View Real-time Logs
```bash
liara logs --app ipicoplus --follow
```

### Check Disk Usage
```bash
liara disk list --app ipicoplus
```

## Scaling

Liara supports horizontal scaling:

```bash
# Scale to 2 instances
liara scale --app ipicoplus --replicas 2
```

## Rollback

If deployment fails:

```bash
# View deployments
liara releases --app ipicoplus

# Rollback to previous version
liara rollback --app ipicoplus --release <release-id>
```

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Deploy to Liara

on:
  push:
    branches: [master]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Deploy to Liara
        env:
          LIARA_TOKEN: ${{ secrets.LIARA_TOKEN }}
        run: |
          npm install -g @liara/cli
          liara deploy --app ipicoplus --api-token $LIARA_TOKEN --detach
```

## Support

- Liara Documentation: https://docs.liara.ir
- Project Issues: Contact development team
- Performance Issues: Check Liara logs and metrics

## Cost Optimization

1. Use appropriate plan size based on traffic
2. Monitor resource usage in Liara dashboard
3. Configure auto-scaling if needed
4. Optimize image size (current: ~200MB)

## Next Steps After Deployment

1. ? Verify app is running: `curl http://your-app.liara.run`
2. ? Test all features: Authentication, CRM, SMS
3. ? Monitor logs for first 24 hours
4. ? Set up alerting in Liara dashboard
5. ? Configure custom domain (optional)
6. ? Enable HTTPS (handled by Liara automatically)

---

**Deployment Date**: 2025-01-15
**Platform**: Liara.ir
**Runtime**: .NET 9.0
**Framework**: Blazor Server
