using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    public class FakeTokenPropertyParser : ITokenPropertyParser
    {
        private readonly Dictionary<string, Errorable<EntitlementTokenProperties>> _outputLookup;

        public FakeTokenPropertyParser(string token, EntitlementTokenProperties result) : this((token, Errorable.Success(result)))
        {
        }

        public FakeTokenPropertyParser(params (string Token, Errorable<EntitlementTokenProperties> Result)[] output)
        {
            _outputLookup = output.ToDictionary(x => x.Token, x => x.Result);
        }

        public Errorable<EntitlementTokenProperties> Parse(string token)
        {
            if (!_outputLookup.TryGetValue(token, out var entitlementResult))
            {
                throw new ArgumentException($"Unexpected token {token}");
            }

            return entitlementResult;
        }
    }
}
