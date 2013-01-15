using System;

namespace FourPiLib.Util
{
	public static class DateTimeHelper
	{
		public static string Ordinal(int day)
		{
			string ordinal = "";
			
			switch(day % 100)
			{
				case 11:
				case 12:
				case 13:
					ordinal = "th";
					break;
			}
			
			if (ordinal == "")
			{
				switch(day % 10)
				{
					case 1:
						ordinal = "st";
						break;
					case 2:
						ordinal = "nd";
						break;
					case 3:
						ordinal = "rd";
						break;
					default:
						ordinal = "th";
						break;
				}
			}
			
			return ordinal;
		}
	}
}

