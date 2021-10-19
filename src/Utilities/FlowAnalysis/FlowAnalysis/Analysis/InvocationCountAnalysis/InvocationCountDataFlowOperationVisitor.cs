﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    using InvocationCountAnalysisData = DictionaryAnalysisData<AnalysisEntity, InvocationCountAnalysisValue>;
    using InvocationCountAnalysisResult = DataFlowAnalysisResult<InvocationCountBlockAnalysisResult, InvocationCountAnalysisValue>;

    internal abstract class InvocationCountDataFlowOperationVisitor : GlobalFlowStateDataFlowOperationVisitor<
        InvocationCountAnalysisData,
        InvocationCountAnalysisContext,
        InvocationCountAnalysisResult,
        InvocationCountAnalysisValue>
    {
        protected InvocationCountDataFlowOperationVisitor(
            InvocationCountAnalysisContext analysisContext) : base(analysisContext, true)
        {
        }

        public override InvocationCountAnalysisData GetEmptyAnalysisData()
            => new();

        private void EnsureInitialized(InvocationCountAnalysisData input)
        {
            if (input.Count == 0)
            {
                input[GlobalEntity] = ValueDomain.Bottom;
            }
            else
            {
                Debug.Assert(input.ContainsKey(GlobalEntity));
            }
        }

        public sealed override (InvocationCountAnalysisData output, bool isFeasibleBranch) FlowBranch(BasicBlock fromBlock, BranchWithInfo branch, InvocationCountAnalysisData input)
        {
            EnsureInitialized(input);
            return base.FlowBranch(fromBlock, branch, input);
        }

        protected override void AddTrackedEntities(InvocationCountAnalysisData analysisData, HashSet<AnalysisEntity> builder, bool forInterproceduralAnalysis = false)
            => builder.UnionWith(analysisData.Keys);

        protected override void ApplyInterproceduralAnalysisResultCore(InvocationCountAnalysisData resultData)
            => ApplyInterproceduralAnalysisResultHelper(resultData);

        protected override void ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(InvocationCountAnalysisData dataAtException, ThrownExceptionInfo throwBranchWithExceptionType)
            => ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(dataAtException, CurrentAnalysisData, throwBranchWithExceptionType);

        protected override bool Equals(InvocationCountAnalysisData value1, InvocationCountAnalysisData value2)
            => InvocationCountAnalysis.Domain.Equals(value1, value2);

        protected override InvocationCountAnalysisValue GetAbstractDefaultValue(ITypeSymbol type)
            => InvocationCountAnalysisValue.Empty;

        protected override InvocationCountAnalysisValue GetAbstractValue(AnalysisEntity analysisEntity)
            => CurrentAnalysisData.TryGetValue(analysisEntity, out var value) ? value : ValueDomain.UnknownOrMayBeValue;

        protected override InvocationCountAnalysisData GetClonedAnalysisData(InvocationCountAnalysisData analysisData)
            => new(analysisData);

        protected override InvocationCountAnalysisData GetExitBlockOutputData(InvocationCountAnalysisResult analysisResult)
            => new(analysisResult.EntryBlockOutput.Data);

        protected override InvocationCountAnalysisData GetTrimmedCurrentAnalysisData(IEnumerable<AnalysisEntity> withEntities)
            => GetTrimmedCurrentAnalysisDataHelper(withEntities, CurrentAnalysisData, SetAbstractValue);

        protected override bool HasAbstractValue(AnalysisEntity analysisEntity)
            => CurrentAnalysisData.ContainsKey(analysisEntity);

        protected override bool HasAnyAbstractValue(InvocationCountAnalysisData data)
            => data.Count > 0;

        protected override InvocationCountAnalysisData MergeAnalysisData(InvocationCountAnalysisData value1, InvocationCountAnalysisData value2)
            => InvocationCountAnalysis.Domain.Intersect(value1, value2, InvocationCountAnalysisValueDomain.Intersect);

        protected override InvocationCountAnalysisData MergeAnalysisDataForBackEdge(InvocationCountAnalysisData value1, InvocationCountAnalysisData value2, BasicBlock forBlock)
            => InvocationCountAnalysis.Domain.Merge(value1, value2);

        protected override void ResetCurrentAnalysisData()
            => ResetAnalysisData(CurrentAnalysisData);

        protected override void SetAbstractValue(AnalysisEntity analysisEntity, InvocationCountAnalysisValue value)
            => SetAbstractValue(CurrentAnalysisData, analysisEntity, value);

        protected override void StopTrackingEntity(AnalysisEntity analysisEntity, InvocationCountAnalysisData analysisData)
            => analysisData.Remove(analysisEntity);

        protected override void UpdateValuesForAnalysisData(InvocationCountAnalysisData targetAnalysisData)
            => UpdateValuesForAnalysisData(targetAnalysisData, CurrentAnalysisData);

        protected static void SetAbstractValue(InvocationCountAnalysisData analysisData, AnalysisEntity analysisEntity, InvocationCountAnalysisValue value)
        {
            if (value.Kind == InvocationCountAnalysisValueKind.Known)
            {
                analysisData[analysisEntity] = value;
            }
        }
    }
}
