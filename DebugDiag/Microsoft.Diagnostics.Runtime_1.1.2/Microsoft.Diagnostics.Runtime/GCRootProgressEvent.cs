namespace Microsoft.Diagnostics.Runtime;

public delegate void GCRootProgressEvent(GCRoot source, long current, long total);
