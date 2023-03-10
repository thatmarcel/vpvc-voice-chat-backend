using System.Text;
using LiteNetLib;
using LiteNetLib.Utils;

namespace VPVC_Voice_Chat_Backend; 

public class VoiceChatServer {
    private EventBasedNetListener listener;
    private NetManager server;

    private List<PeerInfo> allPeers = new();
    private Dictionary<NetPeer, List<PeerInfo>> peerPairs = new();

    public VoiceChatServer() {
        listener = new EventBasedNetListener();
        server = new NetManager(listener);
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

            foreach (var peerPair in new Dictionary<NetPeer, List<PeerInfo>>(peerPairs)) {
                if (peerPair.Key == peer) {
                    peerPairs.Remove(peerPair.Key);
                }

                foreach (var peerInfo in new List<PeerInfo>(peerPair.Value)) {
                    if (peerInfo.peer == peer) {
                        peerPair.Value.Remove(peerInfo);
                    }
                }
            }
        };

        listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) => {
            var receivedBytes = dataReader.GetRemainingBytes();

            if (receivedBytes == null) {
                Logger.LogVerbose("Received no bytes");
                return;
            }
            
            if (peerPairs.ContainsKey(fromPeer)) {
                peerPairs[fromPeer].ForEach(pi => {
                    var writer = new NetDataWriter();
                    writer.Put(pi.id);
                    writer.Put(receivedBytes);
                    pi.peer.Send(writer, DeliveryMethod.Sequenced);
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

                var partyJoinCode = receivedInfoStringParts[0];
                var senderId = receivedInfoStringParts[1];

                if (senderId.Length != 4) {
                    return;
                }
            
                Logger.LogVerbose($"Received sender id: {senderId}, party join code: {partyJoinCode}");

                var peerInfosInParty = allPeers.Where(pi => pi.partyJoinCode == partyJoinCode).ToList();
                
                var peerInfo = new PeerInfo(fromPeer, senderId, partyJoinCode);
                allPeers.Add(peerInfo);

                if (peerInfosInParty.Count > 0) {
                    Logger.LogVerbose($"Found matching party (sender id: {senderId}, party join code: {partyJoinCode})");

                    peerPairs[fromPeer] = peerInfosInParty;

                    foreach (var peerInfoInParty in peerInfosInParty) {
                        if (peerPairs.ContainsKey(peerInfoInParty.peer)) {
                            peerPairs[peerInfoInParty.peer].Add(peerInfo);
                        } else {
                            peerPairs[peerInfoInParty.peer] = new List<PeerInfo> { peerInfo };
                            peerInfoInParty.peer.Send(new byte[] { 1 }, DeliveryMethod.ReliableOrdered);
                        }
                    }
                    
                    fromPeer.Send(new byte[] { 1 }, DeliveryMethod.ReliableOrdered);
                }
            }
        };

        for (;;) {
            server.PollEvents();
            Thread.Sleep(1);
        }
        
        // ReSharper disable once FunctionNeverReturns
    }
}