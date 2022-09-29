namespace Farmtronics.Multiplayer {
	abstract class BaseMessage {		
		public abstract void Apply();
		
		public void Send() {
			MultiplayerManager.SendMessage(this);	
		}
	}
}