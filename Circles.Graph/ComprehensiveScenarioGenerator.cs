using System.Security.Cryptography;
using Circles.Graph.Events;
using Circles.Graph.Rpc;

public static class ComprehensiveScenarioGenerator
{
    private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();
    private static List<string> _accounts = new();
    private static Dictionary<string, string> _tokenByIssuer = new();  // Each account issues its own token

    /// <summary>
    /// Initialize accounts and make each one a token issuer with its own token.
    /// </summary>
    public static void InitializeAccountsAndIssuers(int totalAccounts = 50_000)
    {
        _accounts = GenerateRandomAddresses(totalAccounts);

        // Each account issues its own token
        foreach (var account in _accounts)
        {
            _tokenByIssuer[account] = GenerateRandomToken();
        }
    }

    /// <summary>
    /// Distributes balances such that each account starts with some of its own token
    /// and performs additional transfers to spread tokens through the network.
    /// </summary>
    public static IEnumerable<CirclesEventResult> DistributeBalances(
        int initialBalance = 1_000_000,
        int additionalTransfers = 10_000)
    {
        if (_accounts.Count == 0 || _tokenByIssuer.Count == 0)
            throw new InvalidOperationException("Accounts not initialized. Call InitializeAccountsAndIssuers first.");

        var events = new List<CirclesEventResult>();
        long blockNumber = 2_000_000;
        long timestamp = 1_610_000_000;
        int logCounter = 0;

        // Initialize each account with some of its own token
        foreach (var issuer in _accounts)
        {
            var token = _tokenByIssuer[issuer];
            var initialRecipients = _accounts.Where(a => a != issuer).Take(50).ToList();

            foreach (var recipient in initialRecipients)
            {
                events.Add(MakeTransferEvent("CrcV2_TransferSingle",
                    blockNumber, timestamp, logCounter++, issuer, recipient, token, RandomInt(initialBalance / 2, initialBalance), batchIndex: 0));

                blockNumber += RandomInt(1, 2);
                timestamp += RandomInt(10, 20);
            }
        }

        // Perform additional random transfers to spread tokens
        for (int i = 0; i < additionalTransfers; i++)
        {
            var from = _accounts[RandomInt(_accounts.Count)];
            var to = _accounts[RandomInt(_accounts.Count)];
            if (from == to) continue;

            var token = _tokenByIssuer[from];
            var value = RandomInt(1, 10_000);

            events.Add(MakeTransferEvent("CrcV2_TransferSingle",
                blockNumber, timestamp, logCounter++, from, to, token, value, batchIndex: i % 5));

            blockNumber += RandomInt(1, 2);
            timestamp += RandomInt(10, 20);
        }

        return events;
    }

    /// <summary>
    /// Generates trust events among accounts, ensuring each account can trust multiple others.
    /// </summary>
    public static IEnumerable<CirclesEventResult> GenerateTrustEvents(
        int trustRelationshipsPerAccount = 10)
    {
        if (_accounts.Count == 0) throw new InvalidOperationException("Accounts not initialized.");

        var events = new List<CirclesEventResult>();
        long blockNumber = 3_000_000;
        long timestamp = 1_620_000_000;
        int logCounter = 0;

        foreach (var truster in _accounts)
        {
            var potentialTrustees = _accounts.Where(a => a != truster).Take(trustRelationshipsPerAccount).ToList();

            foreach (var trustee in potentialTrustees)
            {
                var expiryTime = timestamp + RandomInt(3600 * 24 * 365, 3600 * 24 * 365 * 2);

                events.Add(new CirclesEventResult
                {
                    Event = "CrcV2_Trust",
                    Values = new TrustEventValues
                    {
                        BlockNumber = $"0x{blockNumber:X}",
                        Timestamp = $"0x{timestamp:X}",
                        LogIndex = $"0x{logCounter:X}",
                        TransactionIndex = $"0x{(logCounter % 5):X}",
                        TransactionHash = GenerateRandomTransactionHash(),
                        Truster = truster,
                        Trustee = trustee,
                        ExpiryTime = expiryTime.ToString()
                    }
                });

                blockNumber += RandomInt(1, 3);
                timestamp += RandomInt(10, 30);
                logCounter++;
            }
        }

        return events;
    }

    /// <summary>
    /// Generates the complete scenario: balances first, then trust relationships.
    /// </summary>
    public static IEnumerable<CirclesEventResult> GenerateScenario()
    {
        InitializeAccountsAndIssuers();

        var transferEvents = DistributeBalances().ToList();
        var trustEvents = GenerateTrustEvents().ToList();

        // Merge events: transfers first, then trust
        return transferEvents.Concat(trustEvents);
    }

    /// <summary>
    /// Helper to generate a unique token address for each issuer.
    /// </summary>
    private static string GenerateRandomToken()
    {
        var buffer = new byte[20];
        Rng.GetBytes(buffer);
        return "0x" + BitConverter.ToString(buffer).Replace("-", "").ToLower();
    }

    /// <summary>
    /// Helper to create a single transfer event.
    /// </summary>
    private static CirclesEventResult MakeTransferEvent(
        string eventType,
        long blockNumber,
        long timestamp,
        int logIndex,
        string from,
        string to,
        string tokenAddress,
        long value,
        int batchIndex)
    {
        return new CirclesEventResult
        {
            Event = eventType,
            Values = new TransferEventValues
            {
                BlockNumber = $"0x{blockNumber:X}",
                Timestamp = $"0x{timestamp:X}",
                LogIndex = $"0x{logIndex:X}",
                TransactionIndex = $"0x{(logIndex % 5):X}",
                TransactionHash = GenerateRandomTransactionHash(),
                From = from,
                To = to,
                TokenAddress = tokenAddress,
                Value = value.ToString(),
                BatchIndex = $"0x{batchIndex:X}",
                Id = "1"
            }
        };
    }

    private static string GenerateRandomTransactionHash()
    {
        var buffer = new byte[32];
        Rng.GetBytes(buffer);
        return "0x" + BitConverter.ToString(buffer).Replace("-", "").ToLower();
    }

    private static List<string> GenerateRandomAddresses(int count)
    {
        var addresses = new HashSet<string>();
        var buffer = new byte[20];

        while (addresses.Count < count)
        {
            Rng.GetBytes(buffer);
            var address = "0x" + BitConverter.ToString(buffer).Replace("-", "").ToLower();
            addresses.Add(address);
        }

        return addresses.ToList();
    }

    private static int RandomInt(int maxExclusive) => RandomInt(0, maxExclusive);

    private static int RandomInt(int minInclusive, int maxExclusive)
    {
        var buffer = new byte[4];
        Rng.GetBytes(buffer);
        int value = BitConverter.ToInt32(buffer, 0) & 0x7fffffff;
        return (int)((long)value % (maxExclusive - minInclusive)) + minInclusive;
    }
}
