namespace KingmakerSmartSorter
{
    internal sealed class ItemDiagnosticExportResult
    {
        internal bool Success { get; set; }

        internal string OutputPath { get; set; }

        internal string AdditionalOutputPath { get; set; }

        internal string Error { get; set; }

        internal int ItemCount { get; set; }

        internal int BlueprintCount { get; set; }

        internal long FileSize { get; set; }

        internal long AdditionalFileSize { get; set; }
    }
}
