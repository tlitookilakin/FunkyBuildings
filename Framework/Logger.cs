global using static FunkyBuildings.Framework.Logger;
using StardewModdingAPI;

namespace FunkyBuildings.Framework
{
	public class Logger(IMonitor monitor)
	{
		public static Logger Print = null!;

		public void Error(string message)
			=> monitor.Log(message, LogLevel.Error);

		public void Warn(string message)
			=> monitor.Log(message, LogLevel.Warn);

		public void Debug(string message)
			=> monitor.Log(message, LogLevel.Debug);

		public void Trace(string message)
			=> monitor.Log(message, LogLevel.Trace);

		public void Log(string message)
			=> monitor.Log(message, LogLevel.Info);
	}
}
