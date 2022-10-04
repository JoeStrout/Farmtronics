namespace Farmtronics.Multiplayer {
	abstract class BaseMessage<T> where T : BaseMessage<T> {
		public abstract void Apply();

		public void Send(long[] playerIDs = null) {
			MultiplayerManager.SendMessage(this as T, playerIDs);
		}
		
		public void SendToHost() {
			MultiplayerManager.SendMessageToHost(this as T);
		}
	}
}