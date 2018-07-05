using System.Linq;

using Neo.Core;
using Neo.Network.Monitoring;
using Neo.Network.Monitoring.Payloads;

namespace Neo.Network.Monitor
{
    internal class MonitorServerWithWallet : MonitorServer
    {
        private readonly string nodeName;
        private readonly string nodeType;

        public MonitorServerWithWallet(LocalNode localNode, string name, string type)
            : base(localNode)
        {
            this.nodeName = name;
            this.nodeType = type;
        }

        public override CharacterMonitorPayload GetCharacterMonitorPayload()
        {
            var payload = new CharacterMonitorPayload();
            if (Program.Wallet == null)
            {
                return payload;
            }

            var accounts = Program.Wallet.GetAccounts();

            payload.PublicKey = accounts.FirstOrDefault()?.GetKey().PublicKey.ToString() ?? string.Empty;

            payload.IsConsensusNode = Blockchain.Default
                .GetValidators()
                .Any(x => accounts.Any(z => z.GetKey().PublicKey == x));

            payload.Votes = (uint)Blockchain.Default
                .GetEnrollments()
                .Where(x => accounts.Any(z => z.GetKey().PublicKey == x.PublicKey))
                .Select(x => x.Votes)
                .FirstOrDefault();

            var consensusAddress = Blockchain.GetConsensusAddress(Blockchain.Default.GetValidators());

            return payload;
        }

        public override NodeMonitorPayload GetNodeMonitorPayload()
        {
            var lastBlock = Blockchain.Default.GetBlock(Blockchain.Default.Height);
            var payload = new NodeMonitorPayload
            {
                Version = this.localNode.UserAgent,
                Peers = (uint)this.localNode.RemoteNodeCount,
                LastBlockTime = lastBlock.Timestamp,
                UnconfirmedTransactionsCount = (uint)LocalNode.GetMemoryPool().Count(),
                WalletIsOpen = Program.Wallet != null,
                Name = this.nodeName,
                Type = this.nodeType,
                P2PEnabled = this.localNode.ServiceEnabled && this.localNode.RemoteNodeCount > 0,
                RpcEnabled = this.nodeType == "RPC",
                LastBlock = new BlockMonitorPayload
                {
                    Height = lastBlock.Index,
                    Hash = lastBlock.Hash,
                    Transactions = (uint)lastBlock.Transactions.Length
                }
            };

            return payload;
        }
    }
}
