using LiteNetLib;
using LiteNetLib.Utils;

namespace VPVC_Voice_Chat_Backend; 

public class PeerInfo {
    public NetPeer peer;
    public string id;
    public string partyJoinCode;
    public NetDataWriter dataWriter;

    public PeerInfo(NetPeer peer, string id, string partyJoinCode) {
        this.peer = peer;
        this.id = id;
        this.partyJoinCode = partyJoinCode;
        this.dataWriter = new NetDataWriter();
    }
}