using System.Text;
using LiteNetLib;

namespace VPVC_Voice_Chat_Backend; 

public class VoiceChatServer {
    private EventBasedNetListener listener;
    private NetManager server;

    private List<PeerInfo> allPeers = new();
    private Dictionary<NetPeer, Tuple<string, List<PeerInfo>>> peerPairs = new();

    public VoiceChatServer() {
        listener = new EventBasedNetListener();
        server = new NetManager(listener) {
            UnsyncedEvents = true,
            UnsyncedReceiveEvent = true,
            AutoRecycle = true
        };
    }

    public void Start() {
        server.Start(4719);

        listener.ConnectionRequestEvent += request => {
            request.AcceptIfKey("VPVC-Voice-Chat");
        };

        // listener.PeerConnectedEvent += peer => { };
        
        listener.PeerDisconnectedEvent += (peer, disconnectInfo) => {
            foreach (var peerInfo in allPeers) {
                if (peerInfo.peer == peer) {
                    allPeers.Remove(peerInfo);
                    break;
                }
            }

            foreach (var peerPair in new Dictionary<NetPeer, Tuple<string, List<PeerInfo>>>(peerPairs)) {
                if (peerPair.Key == peer) {
                    peerPairs.Remove(peerPair.Key);
                }

                foreach (var peerInfo in new List<PeerInfo>(peerPair.Value.Item2)) {
                    if (peerInfo.peer == peer) {
                        peerPair.Value.Item2.Remove(peerInfo);
                    }
                }
            }
        };

        listener.NetworkReceiveEvent += (fromPeer, dataReader, channel, deliveryMethod) => {
            var receivedBytes = dataReader.GetRemainingBytes();

            if (receivedBytes == null) {
                Logger.LogVerbose("Received no bytes");
                return;
            }
            
            if (peerPairs.ContainsKey(fromPeer)) {
                var peerData = peerPairs[fromPeer];
                peerData.Item2.ForEach(pi => {
                    pi.dataWriter.Reset();
                    pi.dataWriter.Put(peerData.Item1);
                    pi.dataWriter.Put(receivedBytes);
                    pi.peer.Send(pi.dataWriter, DeliveryMethod.Sequenced);
                });
                return;
            }

            if (allPeers.All(pi => pi.peer != fromPeer)) {
                string? receivedInfoString = null;

                try {
                    receivedInfoString = Encoding.UTF8.GetString(receivedBytes, 0, receivedBytes.Length);
                } catch (Exception exception) {
                    Logger.Log(exception.ToString());
                    return;
                }

                var receivedInfoStringParts = receivedInfoString.Split(":");

                if (receivedInfoStringParts.Length != 2) {
                    Logger.LogVerbose($"Received info string part length was incorrect");
                    return;
                }

                var partyIdentifier = receivedInfoStringParts[0];
                var senderId = receivedInfoStringParts[1];

                if (senderId.Length != 4) {
                    return;
                }
            
                Logger.LogVerbose($"Received sender id: {senderId}, party join code: {partyIdentifier}");

                var peerInfosInParty = allPeers.Where(pi => pi.partyIdentifier == partyIdentifier).ToList();
                
                var peerInfo = new PeerInfo(fromPeer, senderId, partyIdentifier);
                allPeers.Add(peerInfo);
                
                peerPairs[fromPeer] = new Tuple<string, List<PeerInfo>>(senderId, peerInfosInParty);

                if (peerInfosInParty.Count > 0) {
                    Logger.LogVerbose($"Found matching party (sender id: {senderId}, party join code: {partyIdentifier})");

                    foreach (var peerInfoInParty in peerInfosInParty) {
                        if (peerPairs.ContainsKey(peerInfoInParty.peer)) {
                            if (!peerPairs[peerInfoInParty.peer].Item2.Contains(peerInfo)) {
                                peerPairs[peerInfoInParty.peer].Item2.Add(peerInfo);
                            }
                        }
                    }
                }
                
                fromPeer.Send(new byte[] { 1 }, DeliveryMethod.ReliableOrdered);
            }
        };
        
        for (;;) { Thread.Sleep(1); }
        
        // ReSharper disable once FunctionNeverReturns
    }
}