namespace Farmtronics.Multiplayer {
	abstract class BaseMessage {
		public abstract void Apply();

		public void Send(long[] playerIDs = null) {
			MultiplayerManager.SendMessage(this, playerIDs);
		}
	}
}