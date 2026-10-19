using Kakuro.Core.Models;

namespace Kakuro.Core.Tests.Models;

public class CandidateCollectionMaskTests : CandidateCollectionContractTests
{
    protected override CandidateCollection Create(params int[] candidates) => new CandidateCollectionMask(candidates);
}
