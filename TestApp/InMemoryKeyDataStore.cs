using System;
using System.Collections.Generic;
using System.Linq;
using BlackFox.U2F.Key;

namespace U2FExperiments
{
    class InMemoryKeyDataStore : IKeyDataStore
    {
        private readonly List<Tuple<byte[], ECKeyPair>> keys = new List<Tuple<byte[], ECKeyPair>>();

        public void StoreKeyPair(byte[] keyHandle, ECKeyPair keyPair)
        {
            keys.Add(Tuple.Create(keyHandle, keyPair));
        }

        public ECKeyPair GetKeyPair(byte[] keyHandle)
        {
            return keys.First(k => k.Item1.SequenceEqual(keyHandle)).Item2;
        }

        private int counter;

        public int IncrementCounter()
        {
            counter += 1;
            return counter;
        }
    }
}
