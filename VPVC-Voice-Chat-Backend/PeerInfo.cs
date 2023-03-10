using LiteNetLib;

namespace VPVC_Voice_Chat_Backend; 

public class PeerInfo {
    public NetPeer peer;
    public string id;
    public string partyJoinCode;

    public PeerInfo(NetPeer peer, string id, string partyJoinCode) {
        this.peer = peer;
        this.id = id;
        this.partyJoinCode = partyJoinCode;
    }
}