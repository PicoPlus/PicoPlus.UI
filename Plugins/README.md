# Plugins Directory

This directory contains plugin DLL files that extend the functionality of PicoPlus.

## How to Add Plugins

1. Build your plugin project (see `/SamplePlugin` for an example)
2. Copy the compiled DLL file to this directory
3. Restart the application
4. Navigate to `/admin/plugins` to manage your plugins

## Plugin Directory Configuration

By default, PicoPlus automatically selects a writable location for plugins:
- **Environment Variable (Recommended)**: Set `PICOPLUS_PLUGINS_PATH` to specify a custom location
- **Default on Linux/macOS**: `/tmp/Plugins` (automatically used if env var not set)
- **Fallback**: `<application-root>/Plugins` (used on Windows or when /tmp is unavailable)

### Cloud Deployments (Liara, Docker, etc.)

If deploying to a read-only filesystem environment, you **must** configure a writable path:

```bash
# Set environment variable to a writable location
export PICOPLUS_PLUGINS_PATH=/tmp/Plugins
```

Or in Docker/docker-compose:
```yaml
environment:
  - PICOPLUS_PLUGINS_PATH=/tmp/Plugins
```

The application will log which directory it selected at startup.

## Plugin State

Plugin enabled/disabled states are automatically saved in `plugin-state.json` in this directory.

## Documentation

For full documentation on creating plugins, see:
- [Plugin System Documentation](../Docs/Plugins/README.md)
- [Sample Plugin](../SamplePlugin/README.md)

