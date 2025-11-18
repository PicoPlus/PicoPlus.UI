# Plugins Directory

This directory contains plugin DLL files that extend the functionality of PicoPlus.

## How to Add Plugins

1. Build your plugin project (see `/SamplePlugin` for an example)
2. Copy the compiled DLL file to this directory
3. Restart the application
4. Navigate to `/admin/plugins` to manage your plugins

## Plugin State

Plugin enabled/disabled states are automatically saved in `plugin-state.json` in this directory.

## Documentation

For full documentation on creating plugins, see:
- [Plugin System Documentation](../Docs/Plugins/README.md)
- [Sample Plugin](../SamplePlugin/README.md)

