using LiteNetLib;
using LiteNetLib.Utils;

namespace VPVC_Voice_Chat_Backend; 

public class PeerInfo {
    public NetPeer peer;
    public string id;
    public string partyIdentifier;
    public NetDataWriter dataWriter;

    public PeerInfo(NetPeer peer, string id, string partyIdentifier) {
        this.peer = peer;
        this.id = id;
        this.partyIdentifier = partyIdentifier;
        this.dataWriter = new NetDataWriter();
    }
}