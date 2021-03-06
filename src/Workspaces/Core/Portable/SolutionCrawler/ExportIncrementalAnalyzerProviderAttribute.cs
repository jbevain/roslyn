// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class ExportIncrementalAnalyzerProviderAttribute : ExportAttribute
    {
        public bool HighPriorityForActiveFile { get; }
        public string Name { get; }
        public string[] WorkspaceKinds { get; }

        public ExportIncrementalAnalyzerProviderAttribute(params string[] workspaceKinds)
            : base(typeof(IIncrementalAnalyzerProvider))
        {
            if (workspaceKinds == null)
            {
                throw new ArgumentNullException(nameof(workspaceKinds));
            }

            // TODO: this will be removed once closed side changes are in.
            this.WorkspaceKinds = workspaceKinds;
            this.Name = "Unknown";
            this.HighPriorityForActiveFile = false;
        }

        public ExportIncrementalAnalyzerProviderAttribute(string name, string[] workspaceKinds)
            : base(typeof(IIncrementalAnalyzerProvider))
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            this.WorkspaceKinds = workspaceKinds;
            this.Name = name;
            this.HighPriorityForActiveFile = false;
        }

        public ExportIncrementalAnalyzerProviderAttribute(bool highPriorityForActiveFile, string name, string[] workspaceKinds)
            : this(name, workspaceKinds)
        {
            this.HighPriorityForActiveFile = highPriorityForActiveFile;
        }
    }
}
