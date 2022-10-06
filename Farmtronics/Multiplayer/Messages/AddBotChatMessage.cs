using Microsoft.Xna.Framework;
using StardewValley;

namespace Farmtronics.Multiplayer.Messages {
	class AddBotChatMessage : BaseMessage<AddBotChatMessage> {
		public string Name { get; set; }
		public string Message { get; set; }
		public Color MsgColor  { get; set; }
		
		public static void Send(string name, string message, Color color) {
			Game1.chatBox.addMessage($"{name}: {message}", color);
			new AddBotChatMessage() {
				Name = name,
				Message = message,
				MsgColor = color
			}.Send();
		}
		
		public override void Apply() {
			Game1.chatBox.addMessage($"{Name}: {Message}", MsgColor);
		}
	}
}