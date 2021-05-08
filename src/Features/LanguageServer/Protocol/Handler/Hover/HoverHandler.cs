﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportLspRequestHandlerProvider, Shared]
    [ProvidesMethod(Methods.TextDocumentHoverName)]
    internal class HoverHandler : AbstractStatelessRequestHandler<TextDocumentPositionParams, Hover?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public HoverHandler()
        {
        }

        public override string Method => Methods.TextDocumentHoverName;

        public override bool MutatesSolutionState => false;
        public override bool RequiresLSPSolution => true;

        public override TextDocumentIdentifier? GetTextDocumentIdentifier(TextDocumentPositionParams request) => request.TextDocument;

        public override async Task<Hover?> HandleRequestAsync(TextDocumentPositionParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            if (document == null)
            {
                return null;
            }

            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

            var quickInfoService = document.Project.LanguageServices.GetRequiredService<QuickInfoService>();
            var info = await quickInfoService.GetQuickInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
            if (info == null)
            {
                return null;
            }

            var supportsVSExtensions = context.ClientCapabilities.HasVisualStudioLspCapability() == true;

            var hover = await GetHoverAsync(info, document, supportsVSExtensions, cancellationToken).ConfigureAwait(false);
            return hover;

            static async Task<Hover> GetHoverAsync(QuickInfoItem info, Document document, bool supportsVSExtensions, CancellationToken cancellationToken)
            {
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                if (supportsVSExtensions)
                {
                    return new VSHover
                    {
                        Range = ProtocolConversions.TextSpanToRange(info.Span, text),
                        Contents = new SumType<SumType<string, MarkedString>, SumType<string, MarkedString>[], MarkupContent>(string.Empty),
                        // Build the classified text without navigation actions - they are not serializable.
                        // TODO - Switch to markup content once it supports classifications.
                        // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/918138
                        RawContent = await IntellisenseQuickInfoBuilder.BuildContentWithoutNavigationActionsAsync(info, document, cancellationToken).ConfigureAwait(false)
                    };
                }
                else
                {
                    return new Hover
                    {
                        Range = ProtocolConversions.TextSpanToRange(info.Span, text),
                        Contents = GetContents(info),
                    };
                }
            }

            static MarkupContent GetContents(QuickInfoItem info)
            {
                // Insert line breaks in between sections to ensure we get double spacing between sections.
                var tags = info.Sections.SelectMany(section => section.TaggedParts.Add(new TaggedText(TextTags.LineBreak, Environment.NewLine))).ToImmutableArray();
                return ProtocolConversions.GetDocumentationMarkdown(tags);
            }
        }
    }
}
