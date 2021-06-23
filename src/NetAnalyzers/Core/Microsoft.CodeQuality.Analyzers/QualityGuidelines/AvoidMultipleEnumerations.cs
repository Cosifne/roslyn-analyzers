// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeQuality.Analyzers.QualityGuidelines
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class AvoidMultipleEnumerations : DiagnosticAnalyzer
    {
        /// <summary>
        /// TODO: How to decide a good rule id.
        /// </summary>
        private const string RuleId = "HAA1838";

        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(MicrosoftCodeQualityAnalyzersResources.AvoidMultipleEnumerationsTitle), MicrosoftCodeQualityAnalyzersResources.ResourceManager, typeof(MicrosoftCodeQualityAnalyzersResources));
        private static readonly LocalizableString s_description = new LocalizableResourceString(nameof(MicrosoftCodeQualityAnalyzersResources.AvoidMultipleEnumerationsMessage), MicrosoftCodeQualityAnalyzersResources.ResourceManager, typeof(MicrosoftCodeQualityAnalyzersResources));

        internal static readonly DiagnosticDescriptor MultipleEnumerableDescriptor = DiagnosticDescriptorHelper.Create(
            RuleId,
            s_localizableTitle,
            s_description,
            DiagnosticCategory.Performance,
            RuleLevel.Disabled,
            description: null,
            isPortedFxCopRule: false,
            isDataflowRule: true);

        private static readonly ImmutableArray<string> s_wellKnownNamesNeedAnalyze = ImmutableArray
            .Create(WellKnownTypeNames.SystemCollectionsGenericIEnumerable1, WellKnownTypeNames.SystemCollectionsIEnumerable);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray<DiagnosticDescriptor>.Empty.Add(MultipleEnumerableDescriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationStartAction(CompliationStartAction);
        }

        private static void CompliationStartAction(CompilationStartAnalysisContext context)
        {
            context.RegisterOperationBlockAction(OperationAction);
        }

        private static void OperationAction(OperationBlockAnalysisContext context)
        {
            var owningSymbol = context.OwningSymbol;
            var cancellationToken = context.CancellationToken;
            var typeSymbolsNeedAnalyze = s_wellKnownNamesNeedAnalyze.Select(name => context.Compilation.GetOrCreateTypeByMetadataName(name)).ToImmutableArray();

            foreach (var operation in context.OperationBlocks)
            {
                if (ShouldAnalyze(operation))
                {
                    var cfg = context.GetControlFlowGraph(operation);
                }
            }

            // TODO : Write the condition
            bool ShouldAnalyze(IOperation operation)
            {
                return true;
            }
        }
    }
}
