using System;
using MonoTouch.UIKit;
using System.IO;

namespace FourPiLib.Util
{
	public static class UIImageHelper
	{
		public static MonoTouch.UIKit.UIImage LoadImage(string filename)
		{
			if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone && UIScreen.MainScreen.Bounds.Height >= 568)
			{
				string tallMagic = "-568h@2x";
				string imagePath = Path.GetDirectoryName(filename);
				string imageFile = Path.GetFileNameWithoutExtension(filename);
				string imageExt = Path.GetExtension(filename);
				
				string iPhone5Image = Path.Combine(imagePath, imageFile + tallMagic + imageExt);
				
				if (File.Exists(iPhone5Image))
				{
					return MonoTouch.UIKit.UIImage.FromFile(iPhone5Image);
				}
				else
				{
					return MonoTouch.UIKit.UIImage.FromFile(filename);
				}
			}
			else
			{
				return MonoTouch.UIKit.UIImage.FromFile(filename);
			}
		}
	}
}

