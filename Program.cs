using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using grom.lib.graphics;
using System.Drawing;

namespace ResizeTest
{
	class Program
	{
		static void Main(string[] args)
		{
			var sourceImagePath = "C:\\temp\\source\\test.jpg";
			var targetImagePath = "C:\\temp\\target\\test.jpg";
			var width = 203;
			var height = 185;
			var quality = 90;

			var fileInfo = new FileInfo(sourceImagePath);

			if (!fileInfo.Exists)
			{
				Console.WriteLine("Error: Source file does not exist");
				return;
			}

			//Important the 2nd parameter useEmbeddedColorManagement, changing this
			//will result in the image being saved with slightly different colours
			var tmpImage = Image.FromFile(fileInfo.FullName, true);
			//Copying the image into a new one in memory should release the handle
			//on the physical file, see: 
			//http://stackoverflow.com/questions/4803935/free-file-locked-by-new-bitmapfilepath/8701748#8701748
			var sourceImage = new Bitmap(tmpImage);
			tmpImage.Dispose();

			byte[] bytes = ImageResizer.Resize(sourceImage, width, height, quality);

			if (bytes == null)
			{
				Console.WriteLine("Error: Target bytes were not generated");
				return;
			}

			using (var f = new FileStream(targetImagePath, FileMode.Create, FileAccess.Write))
			{
				f.Write(bytes, 0, bytes.Length);
				f.Close();
			}

			Console.WriteLine("File generated");
			Console.ReadLine();
		}
	}
}
