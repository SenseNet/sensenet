# SenseNet Index Rebuilder Console Application

The **SenseNet Index Rebuilder** is a standalone console application designed for rebuilding search indexes in sensenet environments. This tool provides advanced progress tracking, dual time estimation, and comprehensive logging capabilities for better operational visibility during index rebuild operations.

## Overview

The Index Rebuilder console application offers a professional alternative to traditional index rebuilding methods with enhanced features:

- **Real-time progress tracking** with percentage completion and node counts
- **Dual ETA estimation** showing both average and worst-case completion times
- **Professional logging** with Serilog integration for monitoring and troubleshooting
- **Phantom activities resolution** to eliminate ghost indexing activities
- **Two rebuild approaches** for different operational needs

## Getting Started

### Prerequisites

- .NET runtime compatible with sensenet
- Valid sensenet database connection
- Write access to the index directory
- Proper connection string configuration

### Configuration

Before running the tool, ensure you have a valid connection string configured in one of these ways:

1. **appsettings.json** file in the application directory
2. **User secrets** for development environments
3. **Environment variables** for containerized deployments

Example appsettings.json:
```json
{
  "ConnectionStrings": {
    "SnCrMsSql": "Data Source=localhost;Initial Catalog=sensenet;User ID=sa;Password=yourpassword;TrustServerCertificate=True"
  }
}
```

### Running the Application

Navigate to the application directory and execute:

```bash
cd src/Tools/SnIndexRebuilder
dotnet run [options]
```

## Command Line Arguments

### Available Options

| Option | Description |
|--------|-------------|
| `--clear-activities` | Performs complete clean rebuild with IndexingActivities table clearing |
| `--help`, `-h` | Shows comprehensive help documentation |
| *(no arguments)* | **Default**: Clean rebuild without clearing activities table |

### Examples

**Standard Clean Rebuild (Recommended)**
```bash
dotnet run
```
This is the default and recommended approach for most scenarios. It performs a clean index rebuild without modifying the IndexingActivities table.

**Complete Clean Rebuild**
```bash
dotnet run --clear-activities
```
Use this approach when experiencing phantom activities issues or when a completely fresh start is required. This will:
- Clear the IndexingActivities table using TRUNCATE + DBCC CHECKIDENT
- Remove cached index directories
- Perform a full rebuild from scratch

**Show Help Documentation**
```bash
dotnet run --help
```

## Rebuild Approaches

### Approach 1: Clean Rebuild (Default)

This is the **recommended approach** for most scenarios:

- Uses `ClearAndPopulateAllAsync` with controlled indexing engine startup
- Prevents phantom activities by disabling indexing during repository startup
- Safer option that doesn't modify database structure
- Suitable for regular maintenance and updates

**When to use:**
- Regular index maintenance
- After content updates or schema changes
- When the IndexingActivities table is clean

### Approach 2: Complete Clean Rebuild (`--clear-activities`)

This approach provides a **complete reset** of the indexing system:

- Truncates the IndexingActivities table and resets identity seed
- Clears index directory to remove cached data
- Eliminates phantom activities (ghost indexing entries)
- Provides the cleanest possible rebuild

**When to use:**
- Experiencing phantom activities issues (1M+ ghost activities)
- After major system migrations or upgrades
- When troubleshooting unexplained indexing behavior
- For establishing a known clean baseline

## Progress Tracking Features

### Real-time Progress Display

The application provides comprehensive progress information:

```
Indexed 25,500 / 62,681 nodes (40.7%) - Elapsed: 00:05:36 - ETA: 00:08:10 (avg) / 00:11:26 (worst)
```

**Progress Information Includes:**
- **Current/Total Nodes**: Number of nodes processed and total count
- **Percentage**: Completion percentage with decimal precision
- **Elapsed Time**: Time spent so far in HH:MM:SS format
- **Dual ETA**: Both average and worst-case time estimates

### Dual ETA Estimation

The tool calculates two time estimates:

- **Average ETA**: Based on current average processing speed
- **Worst-Case ETA**: Based on the maximum time per node encountered

As the process progresses, both estimates typically converge, providing increasingly accurate completion time predictions.

### Update Frequency

Progress updates occur:
- Every **100 nodes** processed
- Every **5 seconds** (whichever comes first)

This provides regular feedback without overwhelming the console output.

## Logging and Monitoring

### Serilog Integration

The application uses Serilog for professional logging with:

- **Console output**: Real-time progress and status messages
- **File logging**: Persistent logs for audit and troubleshooting
- **Structured logging**: JSON-formatted logs for monitoring systems

### Error Handling

Comprehensive error handling includes:

- **Database connection issues**: Graceful handling with clear error messages
- **Index directory problems**: Automatic fallback and warnings
- **Processing errors**: Individual node errors don't stop the entire process
- **Configuration issues**: Clear guidance for resolution

### Log Locations

- **Console**: Real-time output during execution
- **Log Files**: Check the application directory for generated log files
- **Event Logs**: Integration with Windows Event Log (if configured)

## Performance Considerations

### Resource Usage

- **Memory**: Stable memory usage throughout the process
- **CPU**: Moderate CPU usage with efficient processing
- **Disk I/O**: Sequential read/write patterns for optimal performance
- **Database**: Optimized queries with minimal connection overhead

### Large Repositories

For repositories with large node counts:

- Monitor the **worst-case ETA** for realistic time planning
- Consider running during **maintenance windows**
- Ensure adequate **disk space** for index files
- Plan for **extended execution times** (hours for very large repositories)

## Troubleshooting

### Common Issues

**Connection String Problems**
- Verify connection string format and credentials
- Check database server accessibility
- Ensure proper permissions for the database user

**Phantom Activities**
- Use `--clear-activities` option to resolve ghost activities
- Check for previous incomplete rebuild operations
- Verify IndexingActivities table state

**Performance Issues**
- Monitor system resources during execution
- Check disk space availability
- Consider database performance tuning

### Getting Help

Use the built-in help system:
```bash
dotnet run --help
```

This provides comprehensive documentation about all available options and usage examples.

## Best Practices

1. **Always backup** your repository before running index rebuilds
2. **Stop the web application** before running the rebuilder to avoid conflicts
3. **Use the default mode** unless experiencing specific issues
4. **Monitor progress** using the dual ETA estimates for time planning
5. **Check logs** after completion for any reported issues
6. **Verify search functionality** after the rebuild completes

## Technical Details

### Dependencies

- **SenseNet.ContentRepository**: Core repository functionality
- **SenseNet.Search.Lucene29**: Lucene search engine integration
- **Serilog**: Professional logging framework
- **Microsoft.Extensions**: Configuration and dependency injection

### Architecture

The application follows clean architecture principles:

- **IndexingProgressTracker**: Handles progress monitoring and ETA calculation
- **Repository Startup**: Controlled initialization with indexing disabled initially
- **Index Population**: Uses sensenet's built-in index population mechanisms
- **Error Handling**: Comprehensive exception management throughout

This tool provides a robust, professional solution for sensenet index rebuilding operations with enhanced visibility and control over the process.