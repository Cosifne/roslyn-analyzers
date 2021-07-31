﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.AnalyzerUtilities.FlowAnalysis.Analysis.InvocationCountAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeQuality.Analyzers.QualityGuidelines
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed partial class AvoidMultipleEnumerations : DiagnosticAnalyzer
    {
        // TODO: Find a good rule id.
        private const string RuleId = "HAA1838";

        // TODO: Polishing the text here.
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

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray<DiagnosticDescriptor>.Empty.Add(MultipleEnumerableDescriptor);

        public static readonly ImmutableArray<string> ExecutedImmediateMethods = ImmutableArray.Create(
            "System.Linq.Enumerable.Aggregate",
            "System.Linq.Enumerable.All",
            "System.Linq.Enumerable.Any",
            "System.Linq.Enumerable.Average",
            "System.Linq.Enumerable.Contains",
            "System.Linq.Enumerable.Count",
            "System.Linq.Enumerable.ElementAt",
            "System.Linq.Enumerable.ElementAtOrDefault",
            "System.Linq.Enumerable.First",
            "System.Linq.Enumerable.FirstOrDefault",
            "System.Linq.Enumerable.Last",
            "System.Linq.Enumerable.LastOrDefault",
            "System.Linq.Enumerable.LongCount",
            "System.Linq.Enumerable.Max",
            "System.Linq.Enumerable.Min",
            "System.Linq.Enumerable.SequenceEqual",
            "System.Linq.Enumerable.Single",
            "System.Linq.Enumerable.SingleOrDefault",
            "System.Linq.Enumerable.Sum",
            "System.Linq.Enumerable.ToArray",
            "System.Linq.Enumerable.ToDictionary",
            "System.Linq.Enumerable.ToHashSet",
            "System.Linq.Enumerable.ToList",
            "System.Linq.Enumerable.ToLookup");

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationStartAction(CompilationStartAction);
        }

        private static void CompilationStartAction(CompilationStartAnalysisContext context)
        {
            context.RegisterOperationBlockAction(OperationBlockAction);
        }

        private static void OperationBlockAction(OperationBlockAnalysisContext context)
        {
            var iEnumerableType = context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCollectionsGenericIEnumerable1);
            if (iEnumerableType != null)
            {
                Analyze(context, iEnumerableType);
            }
        }

        private static void Analyze(OperationBlockAnalysisContext context, ITypeSymbol iEnumerableType)
        {
            var wellKnowTypeProvider = WellKnownTypeProvider.GetOrCreate(context.Compilation);
            var cfg = context.OperationBlocks.GetControlFlowGraph();
            if (cfg == null)
            {
                return;
            }

            using var blockToTargetInvocationOperationsMapBuilder = PooledDictionary<BasicBlock, ImmutableArray<IInvocationOperation>>.GetInstance();
            var basicBlocks = cfg.Blocks;
            foreach (var block in basicBlocks)
            {
                if (!block.IsReachable || block.Operations.IsEmpty)
                {
                    continue;
                }

                using var arrayBuilder = ArrayBuilder<IInvocationOperation>.GetInstance();
                foreach (var operation in block.Operations)
                {
                    var immediateExecutedInvocationOperations = GetAllImmediateExecutedInvocationOperations(operation, iEnumerableType);
                    if (immediateExecutedInvocationOperations.IsEmpty)
                    {
                        continue;
                    }

                    arrayBuilder.AddRange(immediateExecutedInvocationOperations);
                }

                blockToTargetInvocationOperationsMapBuilder.Add(block, arrayBuilder.ToImmutable());
            }

            if (blockToTargetInvocationOperationsMapBuilder.Count == 0)
            {
                return;
            }

            var blockToTargetInvocationOperationsMap = blockToTargetInvocationOperationsMapBuilder.ToImmutableDictionary();
            var result = InvocationCountAnalysis.TryGetOrComputeResult(
                cfg,
                context.OwningSymbol,
                wellKnowTypeProvider,
                context.Options,
                MultipleEnumerableDescriptor,
                pessimisticAnalysis: false,
                trackingMethodNames: ExecutedImmediateMethods,
                cancellationToken: context.CancellationToken);
            if (result == null)
            {
                return;
            }

            foreach (var (_, invocationOperations) in blockToTargetInvocationOperationsMap)
            {
                // TODO: Test IEnumerable[]
                var multipleEnumerationTargetSet = invocationOperations.WhereAsArray(InvokedMoreThanOneTime)
                    .Select(op => op.Arguments[0].Parameter)
                    .ToImmutableHashSet();
                foreach (var operation in invocationOperations)
                {
                    if (multipleEnumerationTargetSet.Contains(operation.Arguments[0].Parameter))
                    {
                        context.ReportDiagnostic(operation.Arguments[0].CreateDiagnostic(MultipleEnumerableDescriptor));
                    }
                }
            }

            bool InvokedMoreThanOneTime(IInvocationOperation invocationOperation)
            {
                var arguments = invocationOperation.Arguments;
                if (!arguments.IsEmpty)
                {
                    var count = result[invocationOperation.Kind, invocationOperation.Syntax];
                    return count.Kind == InvocationCountAbstractValueKind.MoreThanOneTime;
                }

                return false;
            }
        }

        // TODO: if we decided to go inter-procedural analysis then this needs to be relax.
        private static ImmutableArray<IInvocationOperation> GetAllImmediateExecutedInvocationOperations(IOperation root, ITypeSymbol iEnumerableType)
            => root.Descendants().OfType<IInvocationOperation>().WhereAsArray(op => IsImmediateExecutedLinqOperation(op, iEnumerableType));

        private static bool IsImmediateExecutedLinqOperation(IInvocationOperation operation, ITypeSymbol iEnumerableType)
        {
            var targetMethod = operation.TargetMethod;
            var arguments = operation.Arguments;

            return targetMethod.IsExtensionMethod
                   && ExecutedImmediateMethods.Contains(targetMethod.ToDisplayString(InvocationCountAnalysis.MethodFullyQualifiedNameFormat))
                   && !arguments.IsEmpty
                   && IsLocalIEnumerableOperation(arguments[0], iEnumerableType);
        }

        private static bool IsLocalIEnumerableOperation(IArgumentOperation argumentOperation, ITypeSymbol iEnumerableType)
        {
            var value = argumentOperation.Value;
            return value is ILocalReferenceOperation or IParameterReferenceOperation && value.Type.OriginalDefinition.Equals(iEnumerableType);
        }
    }
}
