using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    public class FakeEntitlementParser : IEntitlementParser
    {
        private readonly Dictionary<string, Errorable<NodeEntitlements>> _outputLookup;

        public FakeEntitlementParser(string token, NodeEntitlements result) : this((token, Errorable.Success(result)))
        {
        }

        public FakeEntitlementParser(params (string Token, Errorable<NodeEntitlements> Result)[] output)
        {
            _outputLookup = output.ToDictionary(x => x.Token, x => x.Result);
        }

        public Errorable<NodeEntitlements> Parse(string token)
        {
            if (!_outputLookup.TryGetValue(token, out var entitlementResult))
            {
                throw new ArgumentException($"Unexpected token {token}");
            }

            return entitlementResult;
        }
    }
}
