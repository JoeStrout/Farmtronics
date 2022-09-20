namespace Farmtronics {
	class BotModData : IModData {
		// mod data keys, used for saving/loading extra data with the game save:
		public static readonly string IS_BOT = "isBot";
		public static readonly string NAME   = "name";
		public static readonly string ENERGY = "energy";
		public static readonly string FACING = "facing";
		
		public bool   IsBot 		  { get; set; }
		public string Name   		  { get; set; } 
		public int 	  Energy 		  { get; set; }
		public int	  FacingDirection { get; set; }
	}
}