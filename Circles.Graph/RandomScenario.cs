using System.Numerics;
using Circles.Common;

namespace Circles.Graph;

public class RandomScenario
{
    /// <summary>
    /// A list of all user addresses in the system.
    /// Every user can also issue tokens and starts with a random balance between 0 and 1000.
    /// The balance can be increased by receiving tokens from other users or decreased by sending tokens to other users.
    /// </summary>
    private readonly HashSet<string> _addresses;

    private readonly string[] _addressArray;

    /// <summary>
    /// A dictionary that stores all outgoing trusts of a user.
    /// The key is the truster's address and the value is a set of trustee addresses.
    /// Actions are 'trust' and 'untrust'.
    /// </summary>
    private readonly Dictionary<string, HashSet<string>> _trusts = new();

    /// <summary>
    /// The current balances of all users. Only users with remaining balances can send tokens.
    /// The first key is the account address (the current holder of a balance).
    /// The value is a nested dictionary with the token address and the account's balance of that token.
    /// </summary>
    private readonly Dictionary<string, Dictionary<string, BigInteger>> _balances = new();

    private long _blockNumber = 0;

    private long Timestamp => DateTimeOffset.Now.ToUnixTimeSeconds() + _blockNumber;

    public RandomScenario(int addressCount)
    {
        _addresses = new HashSet<string>(addressCount);

        InitializeAddresses(addressCount);
        InitializeBalances();

        _addressArray = _addresses.ToArray();
    }

    /// <summary>
    /// Generates random transfer events between random pairs of addresses.
    /// The source must have a balance greater than 0 for any token. Only those senders should be considered in the random selection.
    /// The actual token that will be sent should be chosen randomly from the tokens that the source has.
    /// The block number is increased with each event.
    /// </summary>
    /// <param name="count">How many transfers to produce</param>
    /// <returns></returns>
    public IEnumerable<TransferEvent> GetTransferEvents(int count)
    {
        var sendersWithBalances = _balances
            .Where(kvp => kvp.Value.Any(token => token.Value > 0))
            .ToList();

        if (sendersWithBalances.Count == 0)
        {
            yield break; // No eligible senders
        }
        
        // Yield events for the initial balances (from 00000000-0000-0000-0000-000000000000 to all other addresses)
        foreach (var kvp in _balances)
        {
            foreach (var token in kvp.Value)
            {
                if (token.Value > 0)
                {
                    yield return new TransferEvent(
                        BlockNumber: _blockNumber++,
                        Timestamp: Timestamp,
                        TransactionIndex: 1, // Simplified for this scenario
                        LogIndex: 1, // Simplified for this scenario
                        BatchIndex: 0, // Simplified for this scenario
                        From: "0x0000000000000000000000000000000000000000",
                        To: kvp.Key,
                        TokenAddress: token.Key,
                        Value: token.Value);
                }
            }
        }

        int i = 0;
        while (i < count)
        {
            // Randomly pick a sender with a balance
            var senderEntry = sendersWithBalances[Random.Shared.Next(0, sendersWithBalances.Count - 1)];
            var sender = senderEntry.Key;

            if (senderEntry.Value.Count == 0)
            {
                continue; // No eligible tokens
            }

            // Randomly pick a token from the sender's balances
            var senderTokens = senderEntry.Value.ToArray();
            var tokenEntry = senderTokens[Random.Shared.Next(0, senderTokens.Length)];
            var tokenAddress = tokenEntry.Key;
            var senderBalance = tokenEntry.Value;

            // Randomly pick a receiver
            var receiver = _addressArray[Random.Shared.Next(0, _addresses.Count - 1)];
            while (receiver == sender)
            {
                receiver = _addressArray[Random.Shared.Next(0, _addresses.Count - 1)];
            }

            // Generate a random transfer amount (up to the sender's balance)
            var transferAmount = new BigInteger(Random.Shared.Next(0, (int)senderBalance + 1));

            // Update balances
            if (_balances[sender][tokenAddress] - transferAmount < 0)
            {
                continue; // Not enough balance
            }
            
            _balances[sender][tokenAddress] -= transferAmount;
            if (_balances[sender][tokenAddress] == 0)
            {
                // Remove entries for zero balances
                _balances[sender].Remove(tokenAddress);
            }

            if (!_balances.TryGetValue(receiver, out var receiverBalances))
            {
                receiverBalances = new Dictionary<string, BigInteger>();
                _balances[receiver] = receiverBalances;
            }

            receiverBalances.TryAdd(tokenAddress, 0);
            receiverBalances[tokenAddress] += transferAmount;

            // Yield the TransferEvent
            yield return new TransferEvent(
                BlockNumber: _blockNumber++,
                Timestamp: Timestamp,
                TransactionIndex: 1, // Simplified for this scenario
                LogIndex: 1, // Simplified for this scenario
                BatchIndex: 0, // Simplified for this scenario
                From: sender,
                To: receiver,
                TokenAddress: tokenAddress,
                Value: transferAmount);

            i++;
        }
    }

    /// <summary>
    /// Generates random pairs of addresses and for each looks into the trusts dictionary if the pair is already trusted.
    /// If the pair is not trusted, a new trust is created.
    /// If the pair is already trusted, it's removed.
    /// A 'remove' TrustEvent has it's expiry time set to the current block number.
    /// </summary>
    /// <param name="count">How many trust events to produce (besides the self-trust of every avatar)</param>
    /// <returns></returns>
    public IEnumerable<TrustEvent> GetTrustEvents(int count)
    {
        // Everyone trusts themselves:
        foreach (var address in _addresses)
        {
            yield return new TrustEvent(_blockNumber++, Timestamp, 1, 1, 0, address, address, long.MaxValue);
        }

        // Generate 'count' random trust events:
        for (int i = 0; i < count; i++)
        {
            var truster = _addressArray[Random.Shared.Next(0, _addresses.Count - 1)];
            var trustee = truster;
            while (truster == trustee)
            {
                trustee = _addressArray[Random.Shared.Next(0, _addresses.Count - 1)];
            }

            if (_trusts.TryGetValue(truster, out var trustees))
            {
                if (!trustees.Add(trustee))
                {
                    trustees.Remove(trustee);
                    yield return new TrustEvent(_blockNumber++, Timestamp, 1, 1, 0, truster, trustee, 0);
                }
                else
                {
                    yield return new TrustEvent(_blockNumber++, Timestamp, 1, 1, 0, truster, trustee,
                        long.MaxValue);
                }
            }
            else
            {
                _trusts.Add(truster, [trustee]);
                yield return new TrustEvent(_blockNumber++, Timestamp, 1, 1, 0, truster, trustee,
                    long.MaxValue);
            }
        }
    }

    private void InitializeBalances()
    {
        // Every user gets an initial balance between 0 and 1000
        foreach (var address in _addresses)
        {
            var balance = new BigInteger(Random.Shared.Next(0, 1000));
            _balances.Add(address, new Dictionary<string, BigInteger> { { address, balance } });
        }
    }

    private void InitializeAddresses(int addressCount)
    {
        for (int i = 0; i < addressCount; i++)
        {
            _addresses.Add(Guid.NewGuid().ToString());
        }
    }
}