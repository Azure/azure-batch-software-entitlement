using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    public class FakeTokenPropertyParser : ITokenPropertyParser
    {
        private readonly Dictionary<string, Result<EntitlementTokenProperties, ErrorSet>> _outputLookup;

        public FakeTokenPropertyParser(string token, EntitlementTokenProperties result) : this((token, result))
        {
        }

        public FakeTokenPropertyParser(params (string Token, Result<EntitlementTokenProperties, ErrorSet> Result)[] output)
        {
            _outputLookup = output.ToDictionary(x => x.Token, x => x.Result);
        }

        public Result<EntitlementTokenProperties, ErrorSet> Parse(string token)
        {
            if (!_outputLookup.TryGetValue(token, out var entitlementResult))
            {
                throw new ArgumentException($"Unexpected token {token}");
            }

            return entitlementResult;
        }
    }
}
