; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
RFAA001 | RForge.AutoApi | Error | Marker class must be static partial
RFAA002 | RForge.AutoApi | Warning | No Auto API interfaces found
RFAA003 | RForge.AutoApi | Error | Unsupported return type
RFAA004 | RForge.AutoApi | Error | Generics are not supported
RFAA005 | RForge.AutoApi | Error | ref/out/in parameters are not supported
RFAA006 | RForge.AutoApi | Error | GET/DELETE parameters must be simple types
RFAA007 | RForge.AutoApi | Error | Route placeholders are not supported
RFAA008 | RForge.AutoApi | Error | Duplicate route
RFAA009 | RForge.AutoApi | Warning | Type missing from JsonSerializerContext
RFAA010 | RForge.AutoApi | Warning | Attribute not copied to endpoint
RFAA011 | RForge.AutoApi | Error | Multiple CancellationToken parameters
RFAA012 | RForge.AutoApi | Info | No JsonSerializerContext configured
RFAA013 | RForge.AutoApi | Error | Only methods are supported
RFAA014 | RForge.AutoApi | Error | Only one marker class per project
